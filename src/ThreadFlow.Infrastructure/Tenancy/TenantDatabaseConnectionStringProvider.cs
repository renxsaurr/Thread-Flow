using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ThreadFlow.Infrastructure.Tenancy;

public sealed class TenantDatabaseConnectionStringProvider(
    IConfiguration configuration,
    ICurrentTenantContext currentTenant)
{
    public string GetConnectionString()
    {
        if (!currentTenant.IsResolved)
        {
            throw new InvalidOperationException(
                "A tenant must be resolved before creating TenantDbContext.");
        }

        var serverConnectionString = configuration
            .GetConnectionString("TenantServer")
            ?? throw new InvalidOperationException(
                "Connection string 'TenantServer' is not configured.");

        var connectionString = new SqlConnectionStringBuilder(serverConnectionString)
        {
            InitialCatalog = currentTenant.DatabaseName
        };

        return connectionString.ConnectionString;
    }
}
