using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class SalesApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<SalesCatalogDto> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        return await Http.GetFromJsonAsync<SalesCatalogDto>(
            "api/sales/catalog",
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty sales catalog.");
    }

    public async Task<SaleDto> CreateAsync(
        CreateSaleRequestDto request,
        CancellationToken cancellationToken = default)
    {
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
    decimal UnitPrice);

public sealed record CreateSaleRequestDto(
    Guid BranchId,
    Guid? CustomerId,
    IReadOnlyList<CreateSaleLineRequestDto> Lines);

public sealed record CreateSaleLineRequestDto(Guid ProductId, int Quantity);

public sealed record SaleDto(
    Guid Id,
    string SaleNumber,
    Guid BranchId,
    Guid? CustomerId,
    DateTime OccurredAtUtc,
    decimal TotalAmount,
    IReadOnlyList<SaleLineDto> Lines);

public sealed record SaleLineDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);