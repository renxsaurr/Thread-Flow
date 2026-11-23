namespace ThreadFlow.Domain.Modules.Sales;

public enum PaymentMethod
{
    Cash,
    Card,
    EWallet
}

public sealed class SalePayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}