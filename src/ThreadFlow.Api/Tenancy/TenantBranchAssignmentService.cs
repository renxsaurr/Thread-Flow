using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Master;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Tenancy;

/// <summary>Resolves an employee's branch assignment for this tenant's branch-enabled plan.</summary>
public sealed class TenantBranchAssignmentService(
    MasterDbContext masterDb,
    TenantDbContext tenantDb,
    ICurrentTenantContext currentTenant)
{
    public async Task<Guid?> GetAssignedBranchIdAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
            return null;

        var branchId = await masterDb.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.TenantId == currentTenant.TenantId)
            .Select(user => user.BranchId)
            .SingleOrDefaultAsync(cancellationToken);

        if (branchId is not Guid assignedBranchId)
            return null;

        return await tenantDb.Branches.AnyAsync(
            branch => branch.Id == assignedBranchId && branch.IsActive,
            cancellationToken)
            ? assignedBranchId
            : null;
    }
}
