namespace ThreadFlow.Domain.Modules.Inventory;

/// <summary>Auditable stock reductions, corrections, counts, and inter-branch transfers.</summary>
public sealed class InventoryStockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? DestinationBranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public int Quantity { get; set; }
    public int QuantityChange { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
