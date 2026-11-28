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
    public DbSet<SupplierProduct> SupplierProducts => Set<SupplierProduct>();
    public DbSet<InventoryStockReceipt> InventoryStockReceipts => Set<InventoryStockReceipt>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<InventoryStockMovement> InventoryStockMovements => Set<InventoryStockMovement>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<HeldSale> HeldSales => Set<HeldSale>();
    public DbSet<HeldSaleLine> HeldSaleLines => Set<HeldSaleLine>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnLine> SaleReturnLines => Set<SaleReturnLine>();

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

        modelBuilder.Entity<SupplierProduct>(entity =>
        {
            entity.ToTable("SupplierProducts");
            entity.HasKey(x => new { x.SupplierId, x.ProductId });
            entity.Property(x => x.UnitCost).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.ProductId);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SaleNumber).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.SaleNumber).IsUnique();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountReason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.VoidReason).HasMaxLength(500);
            entity.HasOne<CashierShift>().WithMany().HasForeignKey(x => x.CashierShiftId)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<CashierShift>(entity =>
        {
            entity.ToTable("CashierShifts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.OpeningCash).HasPrecision(18, 2);
            entity.Property(x => x.ClosingCash).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.BranchId, x.OpenedByUserId, x.Status });
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HeldSale>(entity =>
        {
            entity.ToTable("HeldSales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HoldNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(160);
            entity.HasIndex(x => x.HoldNumber).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.HasIndex(x => new { x.BranchId, x.Status, x.CreatedAtUtc });
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.HeldSaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HeldSaleLine>(entity =>
        {
            entity.ToTable("HeldSaleLines");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.HeldSaleId, x.ProductId }).IsUnique();
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.ToTable("SaleReturns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReturnNumber).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.ReturnNumber).IsUnique();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RefundAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.BranchId, x.OccurredAtUtc });
            entity.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SaleReturnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleReturnLine>(entity =>
        {
            entity.ToTable("SaleReturnLines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.HasOne<SaleLine>().WithMany().HasForeignKey(x => x.SaleLineId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.BranchId, x.CreatedAtUtc });
            entity.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("PurchaseOrderLines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryStockMovement>(entity =>
        {
            entity.ToTable("InventoryStockMovements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.BranchId, x.OccurredAtUtc });
            entity.HasIndex(x => x.OperationId);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.DestinationBranchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
