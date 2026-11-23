using System.ComponentModel.DataAnnotations;

namespace ThreadFlow.Api.Authentication;

public sealed class CreateTenantUserRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;

    public Guid? BranchId { get; init; }
}
