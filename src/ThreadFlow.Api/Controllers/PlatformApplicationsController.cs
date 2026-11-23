using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Onboarding;
using ThreadFlow.Domain.Modules.PlatformAdministration;
using ThreadFlow.Domain.Modules.TenantAdministration;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.PlatformAdmin)]
[Route("api/platform/applications")]
public sealed class PlatformApplicationsController(
    MasterDbContext masterDb,
    UserManager<PlatformUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ITenantDatabaseProvisioner databaseProvisioner,
    ILogger<PlatformApplicationsController> logger) : ControllerBase
{
    private static readonly string[] CompanySizes = ["Micro", "Small", "Medium"];
    private const string TenantAdminRole = ThreadFlowRoles.TenantAdmin;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CompanyApplicationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyApplicationResponse>>> GetApplications(
        CancellationToken cancellationToken)
    {
        var applications = await masterDb.CompanyApplications
            .AsNoTracking()
            .OrderBy(application => application.Status == "Pending" ? 0 : 1)
            .ThenByDescending(application => application.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        var response = applications.Select(application => new CompanyApplicationResponse(
            application.Id,
            application.CompanyName,
            application.ContactName,
            application.ContactEmail,
            application.ContactPhone,
            application.RequestedCompanySize,
            application.Status,
            application.SubmittedAtUtc,
            application.TenantId is Guid tenantId
                ? masterDb.Tenants.Where(tenant => tenant.Id == tenantId).Select(tenant => tenant.Code).FirstOrDefault()
                : null,
            application.ReviewNotes)).ToList();

        return Ok(response);
    }

    [HttpPost("{applicationId:guid}/approve")]
    [ProducesResponseType<CompanyApplicationApprovedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyApplicationApprovedResponse>> Approve(
        [FromRoute] Guid applicationId,
        [FromBody] ApproveCompanyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await masterDb.CompanyApplications
            .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken);
        if (application is null)
            return NotFound(new { error = "Company application was not found." });

        Tenant tenant;
        TenantSubscription subscription;
        if (application.Status == "Pending")
        {
            var applicantEmail = application.ContactEmail.Trim();
            if (await userManager.FindByEmailAsync(applicantEmail) is not null)
                return Conflict(new { error = "The applicant email now belongs to a ThreadFlow account." });

            var plan = CompanySizes.FirstOrDefault(value =>
                string.Equals(
                    value,
                    request.ApprovedCompanySize?.Trim() ?? application.RequestedCompanySize,
                    StringComparison.OrdinalIgnoreCase));
            if (plan is null)
                return BadRequest(new { error = "Approved company size must be Micro, Small, or Medium." });

            var databaseGroup = await masterDb.TenantDatabaseGroups
                .AsNoTracking()
                .SingleAsync(group => group.CompanySize == plan, cancellationToken);
            var tenantSuffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var tenantCode = $"TF-{tenantSuffix}";
            var databaseName = $"{databaseGroup.LocalDatabasePrefix}_{tenantSuffix}";
            tenant = new Tenant
            {
                Code = tenantCode,
                Name = application.CompanyName,
                CompanySize = plan,
                DatabaseGroupId = databaseGroup.Id,
                IsActive = false,
                LocalDatabaseName = databaseName,
                CloudDatabaseName = string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            };
            subscription = new TenantSubscription
            {
                TenantId = tenant.Id,
                ApplicationId = application.Id,
                PlanCode = plan,
                Status = "Provisioning",
                RequestedAtUtc = application.SubmittedAtUtc
            };
            masterDb.Tenants.Add(tenant);
            masterDb.TenantSubscriptions.Add(subscription);
            masterDb.TenantCompanySettings.Add(new TenantCompanySettings
            {
                TenantId = tenant.Id,
                LegalName = application.CompanyName,
                BusinessAddress = string.Empty,
                ContactEmail = application.ContactEmail,
                ContactPhone = application.ContactPhone,
                CurrencyCode = "PHP",
                TaxRatePercent = 0m
            });
            application.TenantId = tenant.Id;
            application.Status = "Provisioning";
            application.ReviewedAtUtc = DateTime.UtcNow;
            application.ReviewedByUserId = GetActorId();
            application.ReviewNotes = request.ReviewNotes?.Trim();
            masterDb.PlatformAuditEntries.Add(CreateAudit(
                "CompanyApplicationApproved",
                "CompanyApplication",
                application.Id.ToString(),
                $"Approved {application.CompanyName} for the {plan} plan; provisioning tenant {tenant.Code} in {databaseGroup.Code}.",
                tenant.Id));
            await masterDb.SaveChangesAsync(cancellationToken);
        }
        else if (application.Status == "Provisioning" && application.TenantId is Guid tenantId)
        {
            tenant = await masterDb.Tenants.FirstAsync(item => item.Id == tenantId, cancellationToken);
            subscription = await masterDb.TenantSubscriptions
                .FirstAsync(item => item.ApplicationId == application.Id, cancellationToken);
        }
        else
        {
            return Conflict(new { error = $"Application is already {application.Status}." });
        }

        try
        {
            await databaseProvisioner.ProvisionAsync(tenant.LocalDatabaseName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception,
                "Tenant database provisioning failed for application {ApplicationId}.", application.Id);
            return Problem(
                title: "Tenant database provisioning failed.",
                detail: "The application remains in Provisioning. Retry approval after fixing the database connection or migration issue.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        await using var transaction = await masterDb.Database.BeginTransactionAsync(cancellationToken);
        var email = application.ContactEmail.Trim();
        var tenantAdmin = await userManager.FindByEmailAsync(email);
        if (tenantAdmin is not null &&
            (tenantAdmin.TenantId != tenant.Id || tenantAdmin.IsPlatformAdmin || tenantAdmin.EmailConfirmed))
        {
            return Conflict(new { error = "The applicant email already belongs to another active ThreadFlow account." });
        }

        var tenantAdminRole = await roleManager.FindByNameAsync(TenantAdminRole);
        if (tenantAdminRole is null)
        {
            var createRole = await roleManager.CreateAsync(new IdentityRole<Guid> { Name = TenantAdminRole });
            if (!createRole.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { errors = createRole.Errors.Select(error => error.Description).ToArray() });
        }

        if (tenantAdmin is null)
        {
            tenantAdmin = new PlatformUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                TenantId = tenant.Id,
                IsPlatformAdmin = false
            };
            var createUser = await userManager.CreateAsync(tenantAdmin);
            if (!createUser.Succeeded)
                return BadRequest(new { errors = createUser.Errors.Select(error => error.Description).ToArray() });
        }

        if (!await userManager.IsInRoleAsync(tenantAdmin, TenantAdminRole))
        {
            var addRole = await userManager.AddToRoleAsync(tenantAdmin, TenantAdminRole);
            if (!addRole.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { errors = addRole.Errors.Select(error => error.Description).ToArray() });
        }

        var setupToken = await userManager.GeneratePasswordResetTokenAsync(tenantAdmin);
        tenant.IsActive = true;
        subscription.Status = "Active";
        subscription.ActivatedAtUtc = DateTime.UtcNow;
        application.Status = "Approved";
        await masterDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new CompanyApplicationApprovedResponse(
            application.Id,
            application.CompanyName,
            tenant.Code,
            subscription.PlanCode,
            email,
            setupToken,
            "Active"));
    }

    [HttpPost("{applicationId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid applicationId,
        [FromBody] RejectCompanyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await masterDb.CompanyApplications
            .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken);
        if (application is null)
            return NotFound(new { error = "Company application was not found." });
        if (application.Status != "Pending")
            return Conflict(new { error = $"Application is already {application.Status}." });

        application.Status = "Rejected";
        application.ReviewedAtUtc = DateTime.UtcNow;
        application.ReviewedByUserId = GetActorId();
        application.ReviewNotes = request.Reason.Trim();
        masterDb.PlatformAuditEntries.Add(CreateAudit(
            "CompanyApplicationRejected",
            "CompanyApplication",
            application.Id.ToString(),
            $"Rejected the application from {application.CompanyName}."));
        await masterDb.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? GetActorId() =>
        Guid.TryParse(User.FindFirstValue("sub"), out var userId) ? userId : null;

    private PlatformAuditEntry CreateAudit(
        string action,
        string entityType,
        string entityId,
        string summary,
        Guid? tenantId = null) => new()
    {
        ActorUserId = GetActorId(),
        TenantId = tenantId,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        Summary = summary
    };
}
