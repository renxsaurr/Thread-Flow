namespace ThreadFlow.Domain.Modules.Sales;

/// <summary>Recorded return against a completed sale; returned units are restocked at its branch.</summary>
public sealed class SaleReturn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public Guid BranchId { get; set; }
    public Guid RecordedByUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public List<SaleReturnLine> Lines { get; set; } = [];
}

public sealed class SaleReturnLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleReturnId { get; set; }
    public Guid SaleLineId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
