using System.ComponentModel.DataAnnotations;

namespace ThreadFlow.Api.Authentication;

public sealed class TenantCompanySetupRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string LegalName { get; init; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 2)]
    public string BusinessAddress { get; init; } = string.Empty;

    [StringLength(64)]
    public string? TaxIdentificationNumber { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public string ContactEmail { get; init; } = string.Empty;

    [StringLength(32)]
    public string? ContactPhone { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal TaxRatePercent { get; init; }

    [StringLength(160)]
    public string? ReceiptHeader { get; init; }

    [StringLength(500)]
    public string? ReceiptFooter { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; init; } = "PHP";
}

public sealed class CompleteInvitationRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string TenantCode { get; init; } = string.Empty;

    [Required]
    public string SetupToken { get; init; } = string.Empty;

    [Required, MinLength(10)]
    public string Password { get; init; } = string.Empty;

    public TenantCompanySetupRequest? CompanySettings { get; init; }
}
