using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Crm;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;

public sealed class GetCustomerInteractions(TenantDbContext db)
{
    public Task<List<CustomerInteraction>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        return db.CustomerInteractions
            .AsNoTracking()
            .OrderByDescending(interaction => interaction.SubmittedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}