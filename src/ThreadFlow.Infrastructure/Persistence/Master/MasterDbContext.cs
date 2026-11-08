using Microsoft.EntityFrameworkCore;
using TenantRecord = ThreadFlow.Domain.Modules.TenantAdministration.Tenant;

namespace ThreadFlow.Infrastructure.Persistence.Master;

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
        });
    }
}
