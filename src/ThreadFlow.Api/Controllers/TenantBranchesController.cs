using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Management;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Master;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.TenantOwnerManager + "," + ThreadFlowRoles.BranchManager)]
[Route("api/tenant/branches")]
public sealed class TenantBranchesController(
    TenantDbContext tenantDb,
    MasterDbContext masterDb,
    ICurrentTenantContext currentTenant,
    TenantBranchAssignmentService branchAssignments) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantBranchDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!await IsBranchManagementEnabledAsync(cancellationToken))
            return Forbid();

        await TenantBranchDefaults.EnsureMainBranchAsync(tenantDb, cancellationToken);

        var branchesQuery = tenantDb.Branches
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole(ThreadFlowRoles.BranchManager))
        {
            var assignedBranchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
            if (assignedBranchId is null)
                return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
            branchesQuery = branchesQuery.Where(branch => branch.Id == assignedBranchId.Value);
        }

        var branches = await branchesQuery
            .OrderBy(branch => branch.Name)
            .Select(branch => new TenantBranchDto(
                branch.Id, branch.Code, branch.Name, branch.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(branches);
    }

    [HttpPost]
    public async Task<ActionResult<TenantBranchDto>> Create(
        [FromBody] SaveTenantBranchRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsBranchManagementEnabledAsync(cancellationToken))
            return Forbid();
        if (!User.IsInRole(ThreadFlowRoles.TenantOwnerManager))
            return Forbid();

        var validationError = Validate(request);
        if (validationError is not null)
            return BadRequest(new { error = validationError });

        var branch = new Branch
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            IsActive = true
        };
        tenantDb.Branches.Add(branch);

        try
        {
            await tenantDb.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "That branch code is already in use." });
        }

        return CreatedAtAction(nameof(GetAll), new TenantBranchDto(
            branch.Id, branch.Code, branch.Name, branch.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SaveTenantBranchRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsBranchManagementEnabledAsync(cancellationToken))
            return Forbid();
        if (!User.IsInRole(ThreadFlowRoles.TenantOwnerManager))
            return Forbid();

        var validationError = Validate(request);
        if (validationError is not null)
            return BadRequest(new { error = validationError });

        var branch = await tenantDb.Branches
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (branch is null)
            return NotFound(new { error = "Branch not found." });

        branch.Code = request.Code.Trim().ToUpperInvariant();
        branch.Name = request.Name.Trim();

        try
        {
            await tenantDb.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "That branch code is already in use." });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetTenantBranchStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsBranchManagementEnabledAsync(cancellationToken))
            return Forbid();
        if (!User.IsInRole(ThreadFlowRoles.TenantOwnerManager))
            return Forbid();

        var branch = await tenantDb.Branches
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (branch is null)
            return NotFound(new { error = "Branch not found." });

        if (!request.IsActive && branch.IsActive &&
            !await tenantDb.Branches.AnyAsync(
                item => item.Id != id && item.IsActive,
                cancellationToken))
        {
            return BadRequest(new { error = "At least one branch must remain active." });
        }

        branch.IsActive = request.IsActive;
        await tenantDb.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> IsBranchManagementEnabledAsync(
        CancellationToken cancellationToken)
    {
        var size = await masterDb.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == currentTenant.TenantId)
            .Select(tenant => tenant.CompanySize)
            .SingleOrDefaultAsync(cancellationToken);

        return string.Equals(size, "Medium", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Validate(SaveTenantBranchRequest request)
    {
        var code = request.Code?.Trim();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 32)
            return "Branch code is required and must be 32 characters or fewer.";
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            return "Branch name is required and must be 160 characters or fewer.";
        if (code.Any(char.IsWhiteSpace))
            return "Branch code cannot contain spaces.";
        return null;
    }
}

public sealed record TenantBranchDto(Guid Id, string Code, string Name, bool IsActive);
public sealed record SaveTenantBranchRequest(string Code, string Name);
public sealed record SetTenantBranchStatusRequest(bool IsActive);
