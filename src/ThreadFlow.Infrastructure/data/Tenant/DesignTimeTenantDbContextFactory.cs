using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThreadFlow.Infrastructure.data.Tenant;

public sealed class DesignTimeTenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("THREADFLOW_TENANT_CONNECTION")
            ?? "Server=localhost;Database=ThreadFlow_TenantA;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TenantDbContext(options);
    }
}
