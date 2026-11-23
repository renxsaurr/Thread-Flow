namespace ThreadFlow.Domain.Modules.Inventory;

/// <summary>Records each stock increase and the branch, product, and supplier it came from.</summary>
public sealed class InventoryStockReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
}
