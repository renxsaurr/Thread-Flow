using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Onboarding;

public sealed class TenantDatabaseProvisioner(IConfiguration configuration)
    : ITenantDatabaseProvisioner
{
    public async Task ProvisionAsync(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var serverConnection = configuration.GetConnectionString("TenantServer")
            ?? throw new InvalidOperationException(
                "Connection string 'TenantServer' is not configured.");

        var databaseConnection = new SqlConnectionStringBuilder(serverConnection)
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(databaseConnection)
            .Options;

        await using var tenantDb = new TenantDbContext(options);
        await tenantDb.Database.MigrateAsync(cancellationToken);

        await TenantBranchDefaults.EnsureMainBranchAsync(tenantDb, cancellationToken);
    }
}
