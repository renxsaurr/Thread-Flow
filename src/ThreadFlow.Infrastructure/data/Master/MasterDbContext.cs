using Microsoft.EntityFrameworkCore;
using TenantRecord = ThreadFlow.Domain.Modules.TenantAdministration.Tenant;

namespace ThreadFlow.Infrastructure.data.Master;

public sealed class MasterDbContext(DbContextOptions<MasterDbContext> options) : DbContext(options)
{
    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantRecord>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CompanySize).HasMaxLength(24).IsRequired();
            entity.Property(x => x.LocalDatabaseName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CloudDatabaseName).HasMaxLength(128).IsRequired();

            entity.HasData(
                new TenantRecord
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Code = "TENANT-A",
                    Name = "Tenant A - Micro Company",
                    CompanySize = "Micro",
                    IsActive = true,
                    LocalDatabaseName = "ThreadFlow_TenantA",
                    CloudDatabaseName = "db78629",
                    CreatedAtUtc = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc)
                },
                new TenantRecord
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Code = "TENANT-B",
                    Name = "Tenant B - Small Company",
                    CompanySize = "Small",
                    IsActive = true,
                    LocalDatabaseName = "ThreadFlow_TenantB",
                    CloudDatabaseName = "db78631",
                    CreatedAtUtc = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc)
                },
                new TenantRecord
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Code = "TENANT-C",
                    Name = "Tenant C - Medium Enterprise Company",
                    CompanySize = "Medium",
                    IsActive = true,
                    LocalDatabaseName = "ThreadFlow_TenantC",
                    CloudDatabaseName = "db78630",
                    CreatedAtUtc = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc)
                });
        });
    }
}
