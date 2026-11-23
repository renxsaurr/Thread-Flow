namespace ThreadFlow.Domain.Modules.PlatformAdministration;

public sealed class TenantCompanySettings
{
    public Guid TenantId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
    public string? TaxIdentificationNumber { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public decimal TaxRatePercent { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public string CurrencyCode { get; set; } = "PHP";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}