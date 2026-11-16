using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class CustomerInteractionsApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<List<CustomerInteractionDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await Http.GetFromJsonAsync<List<CustomerInteractionDto>>(
            "api/customer-interactions",
            cancellationToken) ?? [];
    }

    public async Task<CustomerInteractionDto> CreateAsync(
        CreateCustomerInteractionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(
            "api/customer-interactions",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"The API returned {(int)response.StatusCode}: {errorBody}",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<CustomerInteractionDto>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }
}

public sealed record CreateCustomerInteractionRequest(
    Guid? CustomerId,
    string Type,
    string Subject,
    string Details);

public sealed record CustomerInteractionDto(
    Guid Id,
    Guid? CustomerId,
    string Type,
    string Status,
    string Subject,
    string Details,
    DateTime SubmittedAtUtc);