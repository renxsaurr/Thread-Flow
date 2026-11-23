using System.ComponentModel.DataAnnotations;

namespace ThreadFlow.Api.Authentication;

public sealed class CreateTenantAdminRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(10)]
    public string Password { get; init; } = string.Empty;
}
