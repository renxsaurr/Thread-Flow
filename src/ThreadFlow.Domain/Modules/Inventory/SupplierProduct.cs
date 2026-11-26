namespace ThreadFlow.Domain.Modules.Inventory;

/// <summary>A supplier-specific quoted cost for a product in the tenant catalog.</summary>
public sealed class SupplierProduct
{
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
