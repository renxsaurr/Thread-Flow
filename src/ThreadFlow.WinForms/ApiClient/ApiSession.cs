using System.Net.Http.Headers;

namespace ThreadFlow.WinForms.ApiClient;

public static class ApiSession
{
    public static LoginResponseDto? Current { get; private set; }

    public static void Start(LoginResponseDto login) => Current = login;

    public static bool HasRole(string role) =>
        Current?.User.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) == true;

    public static void ApplyAuthorization(HttpClient client)
    {
        var login = Current ?? throw new InvalidOperationException(
            "Sign in before calling the ThreadFlow API.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(login.TokenType, login.AccessToken);
    }
}

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAtUtc,
    SignedInUserDto User);

public sealed record SignedInUserDto(
    string Id,
    string Email,
    string? TenantCode,
    string? CompanySize,
    bool IsPlatformAdmin,
    IReadOnlyList<string> Roles);
