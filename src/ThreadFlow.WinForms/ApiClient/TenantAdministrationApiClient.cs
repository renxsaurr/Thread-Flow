using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class TenantAdministrationApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<List<TenantUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<TenantUserDto>>(
            "api/tenant/users", cancellationToken) ?? [];
    }

    public async Task<List<string>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<string>>(
            "api/tenant/users/roles", cancellationToken) ?? [];
    }

    public async Task<List<TenantUserBranchDto>> GetBranchesAsync(
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<TenantUserBranchDto>>(
            "api/tenant/users/branches", cancellationToken) ?? [];
    }

    public async Task<TenantUserInvitationDto> InviteUserAsync(
        CreateTenantUserDto request, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PostAsJsonAsync(
            "api/tenant/users", request, cancellationToken);
        return await ReadResponseAsync<TenantUserInvitationDto>(response, cancellationToken);
    }

    public async Task AssignBranchAsync(
        Guid userId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PutAsJsonAsync(
            $"api/tenant/users/{userId}/branch",
            new AssignTenantUserBranchDto(branchId),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<TenantCompanySettingsDto> GetCompanySettingsAsync(
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.GetAsync("api/tenant/company-settings", cancellationToken);
        return await ReadResponseAsync<TenantCompanySettingsDto>(response, cancellationToken);
    }

    public async Task SaveCompanySettingsAsync(
        TenantCompanySettingsDto settings, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PutAsJsonAsync(
            "api/tenant/company-settings", settings, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task<T> ReadResponseAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
    }
}

public sealed record TenantUserDto(
    string Id,
    string Email,
    string TenantCode,
    IReadOnlyList<string> Roles,
    bool InvitationPending,
    Guid? BranchId,
    string? BranchName);

public sealed record TenantUserBranchDto(Guid Id, string Code, string Name)
{
    public override string ToString() => Name;
}

public sealed record CreateTenantUserDto(string Email, string Role, Guid? BranchId = null);
public sealed record AssignTenantUserBranchDto(Guid BranchId);

public sealed record TenantUserInvitationDto(
    string Id, string Email, string TenantCode, string Role, Guid? BranchId, string? BranchName, string SetupToken);

public sealed record TenantCompanySettingsDto(
    string LegalName,
    string BusinessAddress,
    string? TaxIdentificationNumber,
    string ContactEmail,
    string? ContactPhone,
    decimal TaxRatePercent,
    string? ReceiptHeader,
    string? ReceiptFooter,
    string CurrencyCode);
