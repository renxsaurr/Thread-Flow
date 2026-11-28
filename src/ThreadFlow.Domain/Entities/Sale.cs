namespace ThreadFlow.Domain.Modules.Sales;

public sealed class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SaleNumber { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? CashierShiftId { get; set; }
    public Guid? CashierUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed";
    public decimal DiscountAmount { get; set; }
    public string DiscountReason { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? VoidReason { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public List<SaleLine> Lines { get; set; } = [];
    public List<SalePayment> Payments { get; set; } = [];
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
