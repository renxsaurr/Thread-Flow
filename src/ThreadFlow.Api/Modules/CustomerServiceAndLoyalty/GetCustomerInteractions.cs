using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Crm;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;

public sealed class GetCustomerInteractions(TenantDbContext db)
{
    public Task<List<CustomerInteraction>> ExecuteAsync(
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var query = db.CustomerInteractions.AsNoTracking();
        if (branchId is Guid selectedBranchId)
            query = query.Where(interaction => interaction.BranchId == selectedBranchId);

        return query
            .OrderByDescending(interaction => interaction.SubmittedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}
