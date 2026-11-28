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

    public async Task<CashierShiftDto?> GetShiftAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.GetAsync($"api/sales/shift?branchId={branchId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CashierShiftDto>(cancellationToken: cancellationToken);
    }

    public Task<CashierShiftDto> OpenShiftAsync(OpenCashierShiftDto request, CancellationToken cancellationToken = default) =>
        SendAsync<CashierShiftDto>("api/sales/shift/open", request, cancellationToken);

    public Task<CashierShiftDto> CloseShiftAsync(Guid id, CloseCashierShiftDto request, CancellationToken cancellationToken = default) =>
        SendAsync<CashierShiftDto>($"api/sales/shift/{id}/close", request, cancellationToken);

    public async Task<List<SalesCustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<SalesCustomerDto>>("api/sales/customers", cancellationToken) ?? [];
    }

    public async Task<List<HeldSaleSummaryDto>> GetHoldsAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<HeldSaleSummaryDto>>($"api/sales/holds?branchId={branchId}", cancellationToken) ?? [];
    }

    public Task<HeldSaleDto> HoldSaleAsync(SaveHeldSaleDto request, CancellationToken cancellationToken = default) =>
        SendAsync<HeldSaleDto>("api/sales/holds", request, cancellationToken);

    public Task<HeldSaleDto> ResumeHoldAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<HeldSaleDto>($"api/sales/holds/{id}/resume", new { }, cancellationToken);

    public async Task CancelHoldAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PostAsync($"api/sales/holds/{id}/cancel", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<SaleHistoryDto>> GetHistoryAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<SaleHistoryDto>>($"api/sales/history?branchId={branchId}", cancellationToken) ?? [];
    }

    public async Task<SaleDetailDto> GetSaleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<SaleDetailDto>($"api/sales/{id}", cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty sale detail.");
    }

    public async Task<List<BranchAvailabilityDto>> GetAvailabilityAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.GetAsync($"api/sales/availability/{productId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<BranchAvailabilityDto>>(cancellationToken: cancellationToken) ?? [];
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
    }

    private static async Task<T> SendAsync<T>(string route, object request, CancellationToken cancellationToken)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PostAsJsonAsync(route, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty response.");
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
    Guid CashierShiftId,
    Guid? CustomerId,
    decimal DiscountAmount,
    string DiscountReason,
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
    string Status,
    decimal DiscountAmount,
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

public sealed record OpenCashierShiftDto(Guid BranchId, decimal OpeningCash);
public sealed record CloseCashierShiftDto(decimal CountedCash);
public sealed record CashierShiftDto(Guid Id, Guid BranchId, DateTime OpenedAtUtc,
    decimal OpeningCash, decimal? CashSales, string Status);
public sealed record SalesCustomerDto(Guid Id, string Name, string? Phone, string? Email);
public sealed record SaveHeldSaleDto(Guid BranchId, Guid? CustomerId, string? CustomerName, IReadOnlyList<CreateSaleLineRequestDto> Lines);
public sealed record HeldSaleSummaryDto(Guid Id, string HoldNumber, DateTime CreatedAtUtc, int Units, string? CustomerName);
public sealed record HeldSaleLineDto(Guid ProductId, int Quantity);
public sealed record HeldSaleDto(Guid Id, string HoldNumber, Guid BranchId, Guid? CustomerId,
    string? CustomerName, IReadOnlyList<HeldSaleLineDto> Lines);
public sealed record SaleHistoryDto(Guid Id, string SaleNumber, DateTime OccurredAtUtc,
    string Status, decimal TotalAmount, int Units, Guid? CustomerId,
    string CashierName, string PaymentMethods);
public sealed record SaleDetailDto(Guid Id, string SaleNumber, Guid BranchId, DateTime OccurredAtUtc,
    string Status, decimal DiscountAmount, string DiscountReason, decimal TotalAmount,
    IReadOnlyList<SaleDetailLineDto> Lines, IReadOnlyList<SalePaymentDto> Payments);
public sealed record SaleDetailLineDto(string Sku, string ProductName, string Color, string Size,
    int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record BranchAvailabilityDto(Guid BranchId, string BranchName, int QuantityOnHand);
