using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class TenantBranchesApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<List<TenantBranchDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await ReadAsync<List<TenantBranchDto>>(
            await Http.GetAsync("api/tenant/branches", cancellationToken), cancellationToken) ?? [];
    }

    public async Task<TenantBranchDto> CreateAsync(
        SaveTenantBranchDto request, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await ReadAsync<TenantBranchDto>(
            await Http.PostAsJsonAsync("api/tenant/branches", request, cancellationToken), cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty branch response.");
    }

    public async Task UpdateAsync(
        Guid id, SaveTenantBranchDto request, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        await EnsureSuccessAsync(
            await Http.PutAsJsonAsync($"api/tenant/branches/{id}", request, cancellationToken), cancellationToken);
    }

    public async Task SetStatusAsync(
        Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/tenant/branches/{id}/status")
        {
            Content = JsonContent.Create(new SetTenantBranchStatusDto(isActive))
        };
        await EnsureSuccessAsync(await Http.SendAsync(request, cancellationToken), cancellationToken);
    }

    private static async Task<T?> ReadAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
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

public sealed record TenantBranchDto(Guid Id, string Code, string Name, bool IsActive);
public sealed record SaveTenantBranchDto(string Code, string Name);
public sealed record SetTenantBranchStatusDto(bool IsActive);
