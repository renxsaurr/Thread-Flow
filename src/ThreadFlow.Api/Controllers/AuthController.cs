using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Domain.Modules.PlatformAdministration;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<PlatformUser> userManager,
    SignInManager<PlatformUser> signInManager,
    MasterDbContext masterDb,
    AccessTokenIssuer tokenIssuer) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid email, password, or tenant." });

        var passwordCheck = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!passwordCheck.Succeeded)
            return Unauthorized(new { error = "Invalid email, password, or tenant." });

        string? tenantCode = null;
        string? companySize = null;
        var requestedTenantCode = request.TenantCode?.Trim();
        if (user.IsPlatformAdmin)
        {
            if (!string.IsNullOrWhiteSpace(requestedTenantCode) || user.TenantId is not null)
                return Unauthorized(new { error = "Invalid email, password, or tenant." });
        }
        else
        {
            if (user.TenantId is not Guid tenantId)
                return Unauthorized(new { error = "Invalid email, password, or tenant." });

            var tenant = await masterDb.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    record => record.Id == tenantId && record.IsActive,
                    cancellationToken);

            if (tenant is null ||
                (!string.IsNullOrWhiteSpace(requestedTenantCode) &&
                 !string.Equals(tenant.Code, requestedTenantCode, StringComparison.OrdinalIgnoreCase)))
            {
                return Unauthorized(new { error = "Invalid email, password, or tenant." });
            }

            tenantCode = tenant.Code;
            companySize = tenant.CompanySize;
        }
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAtUtc) = tokenIssuer.Issue(user, tenantCode, roles.ToArray());
        var response = new LoginResponse(
            accessToken,
            "Bearer",
            Math.Max(0, (int)(expiresAtUtc - DateTime.UtcNow).TotalSeconds),
            expiresAtUtc,
            new SignedInUser(
                user.Id.ToString(),
                user.Email ?? request.Email,
                tenantCode,
                companySize,
                user.IsPlatformAdmin,
                user.IsPlatformAdmin
                    ? roles.Append(ThreadFlowRoles.PlatformAdmin).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : roles.ToArray()));

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("complete-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteInvitation(
        [FromBody] CompleteInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantCode = request.TenantCode.Trim();
        var tenant = await masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.Code == tenantCode && record.IsActive,
                cancellationToken);
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (tenant is null || user is null || user.IsPlatformAdmin ||
            user.TenantId != tenant.Id || user.EmailConfirmed)
        {
            return BadRequest(new { error = "The invitation is invalid or has already been used." });
        }

        var isTenantAdmin = await userManager.IsInRoleAsync(user, ThreadFlowRoles.TenantAdmin);
        if (isTenantAdmin && request.CompanySettings is null)
            return BadRequest(new { error = "Tenant Admin invitation setup must include company settings." });

        await using var transaction = await masterDb.Database.BeginTransactionAsync(cancellationToken);
        var result = await userManager.ResetPasswordAsync(
            user,
            request.SetupToken,
            request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error = "The invitation is invalid or the password does not meet the requirements.",
                details = result.Errors.Select(error => error.Description).ToArray()
            });
        }

        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                error = "The account could not be activated.",
                details = updateResult.Errors.Select(error => error.Description).ToArray()
            });
        }

        masterDb.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = user.Id,
            TenantId = tenant.Id,
            Action = "InvitationCompleted",
            EntityType = "PlatformUser",
            EntityId = user.Id.ToString(),
            Summary = $"The invited account {user.Email} completed password setup for {tenant.Code}."
        });

        if (request.CompanySettings is not null)
        {
            var settings = await masterDb.TenantCompanySettings
                .FirstOrDefaultAsync(item => item.TenantId == tenant.Id, cancellationToken);
            if (settings is null)
            {
                settings = new TenantCompanySettings { TenantId = tenant.Id };
                masterDb.TenantCompanySettings.Add(settings);
            }

            settings.LegalName = request.CompanySettings.LegalName.Trim();
            settings.BusinessAddress = request.CompanySettings.BusinessAddress.Trim();
            settings.TaxIdentificationNumber = request.CompanySettings.TaxIdentificationNumber?.Trim();
            settings.ContactEmail = request.CompanySettings.ContactEmail.Trim();
            settings.ContactPhone = request.CompanySettings.ContactPhone?.Trim();
            settings.TaxRatePercent = request.CompanySettings.TaxRatePercent;
            settings.ReceiptHeader = request.CompanySettings.ReceiptHeader?.Trim();
            settings.ReceiptFooter = request.CompanySettings.ReceiptFooter?.Trim();
            settings.CurrencyCode = request.CompanySettings.CurrencyCode.Trim().ToUpperInvariant();
            settings.UpdatedAtUtc = DateTime.UtcNow;

            masterDb.PlatformAuditEntries.Add(new PlatformAuditEntry
            {
                ActorUserId = user.Id,
                TenantId = tenant.Id,
                Action = "TenantCompanySettingsCompleted",
                EntityType = "TenantCompanySettings",
                EntityId = tenant.Id.ToString(),
                Summary = $"The Tenant Admin completed initial company setup for {tenant.Code}."
            });
        }

        await masterDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}
