using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class TenantCurrencyApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<string> GetCurrencyCodeAsync(CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.GetAsync("api/tenant/currency", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        var currency = await response.Content.ReadFromJsonAsync<TenantCurrencyResponse>(
            cancellationToken: cancellationToken);
        return currency?.CurrencyCode
            ?? throw new InvalidOperationException("The API returned no tenant currency.");
    }
}

public sealed record TenantCurrencyResponse(string CurrencyCode);
