using System.ComponentModel.DataAnnotations;

namespace ThreadFlow.Api.Onboarding;

public sealed class SubmitCompanyApplicationRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string CompanyName { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string ContactName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string ContactEmail { get; init; } = string.Empty;

    [StringLength(32)]
    public string? ContactPhone { get; init; }

    [Required]
    public string RequestedCompanySize { get; init; } = string.Empty;
}

public sealed class ApproveCompanyApplicationRequest
{
    public string? ApprovedCompanySize { get; init; }

    [StringLength(1000)]
    public string? ReviewNotes { get; init; }
}

public sealed class RejectCompanyApplicationRequest
{
    [Required, StringLength(1000, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;
}

public sealed record CompanyApplicationResponse(
    Guid Id,
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string RequestedCompanySize,
    string Status,
    DateTime SubmittedAtUtc,
    string? TenantCode,
    string? ReviewNotes);

public sealed record CompanyApplicationSubmittedResponse(
    Guid Id,
    string Status,
    DateTime SubmittedAtUtc);

public sealed record CompanyApplicationApprovedResponse(
    Guid ApplicationId,
    string CompanyName,
    string TenantCode,
    string CompanySize,
    string TenantAdminEmail,
    string TenantAdminSetupToken,
    string ProvisioningStatus);