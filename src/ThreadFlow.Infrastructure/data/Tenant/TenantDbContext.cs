using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Crm;
using ThreadFlow.Domain.Modules.Management;
using ThreadFlow.Domain.Modules.Sales;
using ThreadFlow.Domain.Modules.Inventory;

namespace ThreadFlow.Infrastructure.data.Tenant;

/// <summary>Business data for exactly one tenant, including both ERP and CRM tables.</summary>
public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerInteraction> CustomerInteractions => Set<CustomerInteraction>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<BranchProductStock> BranchProductStocks => Set<BranchProductStock>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryStockReceipt> InventoryStockReceipts => Set<InventoryStockReceipt>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Phone).HasMaxLength(32);
        });

        modelBuilder.Entity<CustomerInteraction>(entity =>
        {
            entity.ToTable("CustomerInteractions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.Type, x.Status, x.SubmittedAtUtc });
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(48).IsRequired();
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.Color).HasMaxLength(48).IsRequired();
            entity.Property(x => x.Size).HasMaxLength(32).IsRequired();
            entity.HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ContactName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SaleNumber).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.SaleNumber).IsUnique();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleLine>(entity =>
        {
            entity.ToTable("SaleLines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasPrecision(18, 2);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalePayment>(entity =>
        {
            entity.ToTable("SalePayments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Method)
                .HasConversion<string>()
                .HasMaxLength(24)
                .IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.PaidAtUtc).IsRequired();

            entity.HasOne<Sale>()
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchProductStock>(entity =>
        {
            entity.ToTable("BranchProductStocks");
            entity.HasKey(x => new { x.BranchId, x.ProductId });
            entity.Property(x => x.QuantityOnHand).IsRequired();

            entity.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryStockReceipt>(entity =>
        {
            entity.ToTable("InventoryStockReceipts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ReceivedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.BranchId, x.ReceivedAtUtc });
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
