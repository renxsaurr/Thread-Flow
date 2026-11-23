using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Domain.Modules.TenantAdministration;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = PlatformTenantAdminsController.PlatformAdminRole)]
[Route("api/platform/tenants")]
public sealed class PlatformTenantAdminsController(
    MasterDbContext masterDb,
    UserManager<PlatformUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : ControllerBase
{
    public const string PlatformAdminRole = ThreadFlowRoles.PlatformAdmin;
    public const string TenantAdminRole = ThreadFlowRoles.TenantAdmin;

    [HttpPost("{tenantCode}/tenant-admin")]
    [ProducesResponseType<TenantAdminCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantAdminCreatedResponse>> CreateInitialTenantAdmin(
        [FromRoute] string tenantCode,
        [FromBody] CreateTenantAdminRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedTenantCode = tenantCode.Trim();
        var tenant = await masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.Code == normalizedTenantCode && record.IsActive,
                cancellationToken);

        if (tenant is null)
            return NotFound(new { error = "The tenant was not found or is inactive." });

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Conflict(new { error = "An account already uses this email address." });

        await using var transaction = await masterDb.Database.BeginTransactionAsync(cancellationToken);

        var tenantAdminRole = await roleManager.FindByNameAsync(TenantAdminRole);
        if (tenantAdminRole is null)
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole<Guid> { Name = TenantAdminRole });
            if (!roleResult.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { errors = roleResult.Errors.Select(error => error.Description).ToArray() });
            }
        }

        var existingTenantAdmins = await userManager.GetUsersInRoleAsync(TenantAdminRole);
        if (existingTenantAdmins.Any(user => user.TenantId == tenant.Id))
            return Conflict(new { error = "This tenant already has an initial Tenant Admin." });

        var tenantAdmin = new PlatformUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenant.Id,
            IsPlatformAdmin = false
        };

        var createResult = await userManager.CreateAsync(tenantAdmin, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors.Select(error => error.Description).ToArray()
            });
        }

        var roleAssignment = await userManager.AddToRoleAsync(tenantAdmin, TenantAdminRole);
        if (!roleAssignment.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { errors = roleAssignment.Errors.Select(error => error.Description).ToArray() });
        }

        await transaction.CommitAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new TenantAdminCreatedResponse(
                tenantAdmin.Id.ToString(),
                email,
                tenant.Code,
                TenantAdminRole));
    }
}
