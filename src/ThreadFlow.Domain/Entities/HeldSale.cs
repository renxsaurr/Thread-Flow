namespace ThreadFlow.Domain.Modules.Sales;

/// <summary>An unpaid cart saved so the cashier can resume it later.</summary>
public sealed class HeldSale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string HoldNumber { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid CashierUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResumedAtUtc { get; set; }
    public string Status { get; set; } = "Held";
    public List<HeldSaleLine> Lines { get; set; } = [];
}

public sealed class HeldSaleLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HeldSaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
