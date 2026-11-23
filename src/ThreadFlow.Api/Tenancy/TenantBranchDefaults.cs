using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Management;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Tenancy;

public static class TenantBranchDefaults
{
    public static async Task EnsureMainBranchAsync(
        TenantDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.Branches.AnyAsync(cancellationToken))
            return;

        var mainBranch = new Branch
        {
            Code = "MAIN",
            Name = "Main Branch",
            IsActive = true
        };
        db.Branches.Add(mainBranch);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(mainBranch).State = EntityState.Detached;
            if (!await db.Branches.AnyAsync(cancellationToken))
                throw;
        }
    }
}
