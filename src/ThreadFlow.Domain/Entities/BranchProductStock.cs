namespace ThreadFlow.Domain.Modules.Inventory;

public sealed class BranchProductStock
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityOnHand { get; set; }
}