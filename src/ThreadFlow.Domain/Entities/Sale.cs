namespace ThreadFlow.Domain.Modules.Sales;

public sealed class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SaleNumber { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public List<SaleLine> Lines { get; set; } = [];
}

public sealed class SaleLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
