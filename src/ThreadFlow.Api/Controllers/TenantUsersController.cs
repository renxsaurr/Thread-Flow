using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Management;
using ThreadFlow.Domain.Modules.PlatformAdministration;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Master;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.TenantAdmin)]
[Route("api/tenant/users")]
public sealed class TenantUsersController(
    UserManager<PlatformUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    MasterDbContext masterDb,
    TenantDbContext tenantDb,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpGet("roles")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<string>> GetAssignableRoles()
    {
        var assignableRoles = IsBranchEnabled()
            ? ThreadFlowRoles.TenantAssignableEmployeeRoles
            : ThreadFlowRoles.TenantAssignableEmployeeRoles
                .Where(role => !string.Equals(
                    role,
                    ThreadFlowRoles.BranchManager,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
        return Ok(assignableRoles);
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyList<TenantUserBranchOption>>> GetBranches(
        CancellationToken cancellationToken)
    {
        if (!IsBranchEnabled())
            return Ok(Array.Empty<TenantUserBranchOption>());

        await TenantBranchDefaults.EnsureMainBranchAsync(tenantDb, cancellationToken);
        var branches = await tenantDb.Branches
            .AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Name)
            .Select(branch => new TenantUserBranchOption(branch.Id, branch.Code, branch.Name))
            .ToListAsync(cancellationToken);

        return Ok(branches);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TenantUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TenantUserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Where(user => user.TenantId == currentTenant.TenantId && !user.IsPlatformAdmin)
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var assignedBranchIds = users
            .Where(user => user.BranchId.HasValue)
            .Select(user => user.BranchId!.Value)
            .Distinct()
            .ToArray();
        var branchNames = assignedBranchIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await tenantDb.Branches
                .AsNoTracking()
                .Where(branch => assignedBranchIds.Contains(branch.Id))
                .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var response = new List<TenantUserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            response.Add(new TenantUserResponse(
                user.Id.ToString(),
                user.Email ?? string.Empty,
                currentTenant.Code,
                roles.ToArray(),
                !user.EmailConfirmed,
                user.BranchId,
                user.BranchId is Guid branchId && branchNames.TryGetValue(branchId, out var branchName)
                    ? branchName
                    : null));
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<CreateTenantUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateTenantUserResponse>> CreateUser(
        [FromBody] CreateTenantUserRequest request,
        CancellationToken cancellationToken)
    {
        var role = ThreadFlowRoles.TenantAssignableEmployeeRoles.FirstOrDefault(
            allowedRole => string.Equals(
                allowedRole,
                request.Role.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            return BadRequest(new
            {
                error = "Role must be one of the assignable tenant employee roles.",
                allowedRoles = ThreadFlowRoles.TenantAssignableEmployeeRoles
            });
        }

        if (!IsBranchEnabled() && string.Equals(
                role,
                ThreadFlowRoles.BranchManager,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Branch Manager is only available for Medium-plan tenants." });
        }

        var branchValidation = await ValidateBranchAssignmentAsync(role, request.BranchId, cancellationToken);
        if (branchValidation.Error is not null)
            return BadRequest(new { error = branchValidation.Error });

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Conflict(new { error = "An account already uses this email address." });

        await using var transaction = await masterDb.Database.BeginTransactionAsync(cancellationToken);
        var tenantRole = await roleManager.FindByNameAsync(role);
        if (tenantRole is null)
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
            if (!roleResult.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { errors = roleResult.Errors.Select(error => error.Description).ToArray() });
            }
        }

        var user = new PlatformUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            TenantId = currentTenant.TenantId,
            BranchId = branchValidation.Branch?.Id,
            IsPlatformAdmin = false
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors.Select(error => error.Description).ToArray()
            });
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, role);
        if (!roleAssignment.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { errors = roleAssignment.Errors.Select(error => error.Description).ToArray() });
        }

        var setupToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var actorId = Guid.TryParse(User.FindFirst("sub")?.Value, out var parsedActorId)
            ? parsedActorId
            : (Guid?)null;
        masterDb.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = actorId,
            TenantId = currentTenant.TenantId,
            Action = "TenantEmployeeInvited",
            EntityType = "PlatformUser",
            EntityId = user.Id.ToString(),
            Summary = $"A {role} invitation was created for {email} in {currentTenant.Code}."
        });
        await masterDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateTenantUserResponse(
                user.Id.ToString(),
                email,
                currentTenant.Code,
                role,
                branchValidation.Branch?.Id,
                branchValidation.Branch?.Name,
                setupToken));
    }

    [HttpPut("{id:guid}/branch")]
    public async Task<IActionResult> AssignBranch(
        Guid id,
        [FromBody] AssignTenantUserBranchRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsBranchEnabled())
            return BadRequest(new { error = "Branch assignments are available for Medium-plan tenants." });

        var user = await userManager.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentTenant.TenantId,
                cancellationToken);
        if (user is null || user.IsPlatformAdmin)
            return NotFound(new { error = "Tenant employee was not found." });

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(ThreadFlowRoles.RequiresBranchAssignment))
            return BadRequest(new { error = "This role does not require a branch assignment." });

        var branch = await tenantDb.Branches
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.BranchId && candidate.IsActive,
                cancellationToken);
        if (branch is null)
            return BadRequest(new { error = "Choose an active branch in this company." });

        await using var transaction = await masterDb.Database.BeginTransactionAsync(cancellationToken);
        user.BranchId = branch.Id;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = updateResult.Errors.Select(error => error.Description).ToArray()
            });
        }

        var actorId = Guid.TryParse(User.FindFirst("sub")?.Value, out var parsedActorId)
            ? parsedActorId
            : (Guid?)null;
        masterDb.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = actorId,
            TenantId = currentTenant.TenantId,
            Action = "TenantEmployeeBranchAssigned",
            EntityType = "PlatformUser",
            EntityId = user.Id.ToString(),
            Summary = $"{user.Email} was assigned to the {branch.Name} branch in {currentTenant.Code}."
        });
        await masterDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return NoContent();
    }

    private bool IsBranchEnabled() =>
        string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase);

    private async Task<(Branch? Branch, string? Error)> ValidateBranchAssignmentAsync(
        string role,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (!IsBranchEnabled())
        {
            return branchId is null
                ? (null, null)
                : (null, "This tenant plan does not support branch assignments.");
        }

        if (!ThreadFlowRoles.RequiresBranchAssignment(role))
        {
            return branchId is null
                ? (null, null)
                : (null, "Tenant Owner/Manager accounts are company-wide and do not have a branch assignment.");
        }

        if (branchId is not Guid selectedBranchId)
            return (null, "Choose a branch for this employee.");

        var branch = await tenantDb.Branches
            .SingleOrDefaultAsync(
                candidate => candidate.Id == selectedBranchId && candidate.IsActive,
                cancellationToken);
        return branch is null
            ? (null, "Choose an active branch in this company.")
            : (branch, null);
    }
}

public sealed record TenantUserBranchOption(Guid Id, string Code, string Name);
public sealed record AssignTenantUserBranchRequest(Guid BranchId);
