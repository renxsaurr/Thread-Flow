using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class SalesApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<SalesCatalogDto> GetCatalogAsync(
        Guid? branchId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        var queryParts = new List<string>();

        if (branchId is Guid selectedBranchId)
            queryParts.Add($"branchId={selectedBranchId}");

        if (!string.IsNullOrWhiteSpace(searchTerm))
            queryParts.Add(
                $"searchTerm={Uri.EscapeDataString(searchTerm.Trim())}");

        var requestUri = "api/sales/catalog";

        if (queryParts.Count > 0)
            requestUri += "?" + string.Join("&", queryParts);

        return await Http.GetFromJsonAsync<SalesCatalogDto>(
            requestUri,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty sales catalog.");
    }

    public async Task<SaleDto> CreateAsync(
        CreateSaleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PostAsJsonAsync(
            "api/sales",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"The API returned {(int)response.StatusCode}: {errorBody}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<SaleDto>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty sale response.");
    }
}

public sealed record SalesCatalogDto(
    IReadOnlyList<SalesBranchDto> Branches,
    IReadOnlyList<SalesProductDto> Products);

public sealed record SalesBranchDto(Guid Id, string Code, string Name);

public sealed record SalesProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Color,
    string Size,
    decimal UnitPrice,
    int QuantityOnHand);

public sealed record CreateSaleRequestDto(
    Guid BranchId,
    Guid? CustomerId,
    IReadOnlyList<CreateSaleLineRequestDto> Lines,
    IReadOnlyList<CreateSalePaymentRequestDto> Payments);

public sealed record CreateSaleLineRequestDto(Guid ProductId, int Quantity);
public sealed record CreateSalePaymentRequestDto(string Method, decimal Amount);

public sealed record SaleDto(
    Guid Id,
    string SaleNumber,
    Guid BranchId,
    Guid? CustomerId,
    DateTime OccurredAtUtc,
    decimal TotalAmount,
    IReadOnlyList<SaleLineDto> Lines,
    IReadOnlyList<SalePaymentDto> Payments);

public sealed record SaleLineDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record SalePaymentDto(
    string Method,
    decimal Amount,
    DateTime PaidAtUtc);
