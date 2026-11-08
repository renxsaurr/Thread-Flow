using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThreadFlow.Infrastructure.Persistence.Master;

public sealed class DesignTimeMasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("THREADFLOW_MASTER_CONNECTION")
            ?? "Server=localhost;Database=ThreadFlow_Master;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new MasterDbContext(options);
    }
}
