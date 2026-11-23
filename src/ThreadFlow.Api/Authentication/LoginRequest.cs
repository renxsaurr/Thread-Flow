using System.ComponentModel.DataAnnotations;

namespace ThreadFlow.Api.Authentication;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    // Optional tenant check for clients that already know the tenant code. The API derives it from the user account when omitted.
    public string? TenantCode { get; init; }
}
