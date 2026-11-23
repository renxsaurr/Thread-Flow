using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class AuthApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task CompleteInvitationAsync(
        CompleteTenantInvitationDto request,
        CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(
            "api/auth/complete-invitation", request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
    }
    public async Task<LoginResponseDto> LoginAsync(
        string email,
        string password,
        string? tenantCode,
        CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequestDto(email, password, tenantCode),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Sign in failed ({(int)response.StatusCode}): {errorBody}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<LoginResponseDto>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty login response.");
    }
}

public sealed record LoginRequestDto(string Email, string Password, string? TenantCode);
public sealed record CompleteTenantInvitationDto(
    string Email,
    string TenantCode,
    string SetupToken,
    string Password,
    TenantCompanySettingsDto? CompanySettings);