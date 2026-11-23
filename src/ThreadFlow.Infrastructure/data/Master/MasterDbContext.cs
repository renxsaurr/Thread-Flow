using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.PlatformAdministration;
using ThreadFlow.Domain.Modules.TenantAdministration;
using TenantRecord = ThreadFlow.Domain.Modules.TenantAdministration.Tenant;

namespace ThreadFlow.Infrastructure.data.Master;

public sealed class MasterDbContext(DbContextOptions<MasterDbContext> options)
    : IdentityDbContext<PlatformUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();
    public DbSet<TenantDatabaseGroup> TenantDatabaseGroups => Set<TenantDatabaseGroup>();
    public DbSet<CompanyApplication> CompanyApplications => Set<CompanyApplication>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<TenantCompanySettings> TenantCompanySettings => Set<TenantCompanySettings>();
    public DbSet<PlatformAuditEntry> PlatformAuditEntries => Set<PlatformAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantDatabaseGroup>(entity =>
        {
            entity.ToTable("TenantDatabaseGroups");
            entity.HasKey(group => group.Id);
            entity.Property(group => group.Code).HasMaxLength(32).IsRequired();
            entity.HasIndex(group => group.Code).IsUnique();
            entity.Property(group => group.Name).HasMaxLength(120).IsRequired();
            entity.Property(group => group.CompanySize).HasMaxLength(24).IsRequired();
            entity.HasIndex(group => group.CompanySize).IsUnique();
            entity.Property(group => group.LocalDatabasePrefix).HasMaxLength(96).IsRequired();
            entity.HasData(
                new TenantDatabaseGroup
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Code = "TENANT-A",
                    Name = "Tenant A — Micro hosting group",
                    CompanySize = "Micro",
                    LocalDatabasePrefix = "ThreadFlow_TenantA"
                },
                new TenantDatabaseGroup
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    Code = "TENANT-B",
                    Name = "Tenant B — Small hosting group",
                    CompanySize = "Small",
                    LocalDatabasePrefix = "ThreadFlow_TenantB"
                },
                new TenantDatabaseGroup
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                    Code = "TENANT-C",
                    Name = "Tenant C — Medium hosting group",
                    CompanySize = "Medium",
                    LocalDatabasePrefix = "ThreadFlow_TenantC"
                });
        });

        modelBuilder.Entity<TenantRecord>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CompanySize).HasMaxLength(24).IsRequired();
            entity.HasIndex(x => x.DatabaseGroupId);
            entity.HasOne<TenantDatabaseGroup>()
                .WithMany()
                .HasForeignKey(x => x.DatabaseGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.LocalDatabaseName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CloudDatabaseName).HasMaxLength(128).IsRequired();

            entity.HasData(
                new TenantRecord
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Code = "TENANT-A",
                    Name = "Tenant A - Micro Company",
                    CompanySize = "Micro",
                    DatabaseGroupId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
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
                    DatabaseGroupId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
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
                    DatabaseGroupId = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                    IsActive = true,
                    LocalDatabaseName = "ThreadFlow_TenantC",
                    CloudDatabaseName = "db78630",
                    CreatedAtUtc = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc)
                });
        });

        modelBuilder.Entity<CompanyApplication>(entity =>
        {
            entity.ToTable("CompanyApplications");
            entity.HasKey(application => application.Id);
            entity.Property(application => application.CompanyName).HasMaxLength(160).IsRequired();
            entity.Property(application => application.ContactName).HasMaxLength(160).IsRequired();
            entity.Property(application => application.ContactEmail).HasMaxLength(254).IsRequired();
            entity.Property(application => application.ContactPhone).HasMaxLength(32);
            entity.Property(application => application.RequestedCompanySize).HasMaxLength(24).IsRequired();
            entity.Property(application => application.Status).HasMaxLength(24).IsRequired();
            entity.Property(application => application.ReviewNotes).HasMaxLength(1000);
            entity.HasIndex(application => new { application.Status, application.SubmittedAtUtc });
            entity.HasOne<TenantRecord>().WithMany().HasForeignKey(application => application.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantSubscription>(entity =>
        {
            entity.ToTable("TenantSubscriptions");
            entity.HasKey(subscription => subscription.Id);
            entity.Property(subscription => subscription.PlanCode).HasMaxLength(24).IsRequired();
            entity.Property(subscription => subscription.Status).HasMaxLength(24).IsRequired();
            entity.HasIndex(subscription => subscription.ApplicationId).IsUnique();
            entity.HasIndex(subscription => new { subscription.TenantId, subscription.Status });
            entity.HasOne<TenantRecord>().WithMany().HasForeignKey(subscription => subscription.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CompanyApplication>().WithMany().HasForeignKey(subscription => subscription.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantCompanySettings>(entity =>
        {
            entity.ToTable("TenantCompanySettings");
            entity.HasKey(settings => settings.TenantId);
            entity.Property(settings => settings.LegalName).HasMaxLength(160).IsRequired();
            entity.Property(settings => settings.BusinessAddress).HasMaxLength(500).IsRequired();
            entity.Property(settings => settings.TaxIdentificationNumber).HasMaxLength(64);
            entity.Property(settings => settings.ContactEmail).HasMaxLength(254).IsRequired();
            entity.Property(settings => settings.ContactPhone).HasMaxLength(32);
            entity.Property(settings => settings.TaxRatePercent).HasPrecision(5, 2);
            entity.Property(settings => settings.ReceiptHeader).HasMaxLength(160);
            entity.Property(settings => settings.ReceiptFooter).HasMaxLength(500);
            entity.Property(settings => settings.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.HasOne<TenantRecord>().WithMany().HasForeignKey(settings => settings.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformAuditEntry>(entity =>
        {
            entity.ToTable("PlatformAuditEntries");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Action).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.EntityId).HasMaxLength(128).IsRequired();
            entity.Property(entry => entry.Summary).HasMaxLength(500).IsRequired();
            entity.HasIndex(entry => entry.OccurredAtUtc);
            entity.HasIndex(entry => new { entry.EntityType, entry.EntityId });
            entity.HasIndex(entry => entry.TenantId);
            entity.HasOne<TenantRecord>()
                .WithMany()
                .HasForeignKey(entry => entry.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformUser>(entity =>
        {
            entity.Property(user => user.TenantId).IsRequired(false);
            entity.Property(user => user.IsPlatformAdmin).IsRequired();
            entity.HasIndex(user => user.TenantId);
            entity.HasOne<TenantRecord>()
                .WithMany()
                .HasForeignKey(user => user.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("AspNetUsers", table => table.HasCheckConstraint(
                "CK_AspNetUsers_PlatformOrTenantUser",
                "([IsPlatformAdmin] = 1 AND [TenantId] IS NULL) OR " +
                "([IsPlatformAdmin] = 0 AND [TenantId] IS NOT NULL)"));
        });
    }
}
