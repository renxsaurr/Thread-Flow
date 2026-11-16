using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class SalesReportsApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<SalesOperationalReportDto> GetSalesReportAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var requestUri =
            "api/reports/sales?" +
            $"fromUtc={FormatQueryValue(fromUtc)}&" +
            $"toUtcExclusive={FormatQueryValue(toUtcExclusive)}";

        using var response = await Http.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"The API returned {(int)response.StatusCode}: {errorBody}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<SalesOperationalReportDto>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty sales report.");
    }

    private static string FormatQueryValue(DateTimeOffset value)
    {
        var utcValue = value.ToUniversalTime().ToString("O");
        return Uri.EscapeDataString(utcValue);
    }
}

public sealed record SalesOperationalReportDto(
    DateTime FromUtc,
    DateTime ToUtcExclusive,
    int SaleCount,
    int UnitsSold,
    decimal TotalSalesAmount);