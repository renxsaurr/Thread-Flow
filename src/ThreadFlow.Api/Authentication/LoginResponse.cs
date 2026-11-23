namespace ThreadFlow.Api.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAtUtc,
    SignedInUser User);

public sealed record SignedInUser(
    string Id,
    string Email,
    string? TenantCode,
    string? CompanySize,
    bool IsPlatformAdmin,
    IReadOnlyList<string> Roles);
