using System.Net.Http.Json;

namespace ThreadFlow.WinForms.ApiClient;

public sealed class OnboardingApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:5086/")
    };

    public async Task<CompanyApplicationSubmittedDto> SubmitApplicationAsync(
        SubmitCompanyApplicationDto request,
        CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(
            "api/onboarding/applications", request, cancellationToken);
        return await ReadResponseAsync<CompanyApplicationSubmittedDto>(response, cancellationToken);
    }

    public async Task<List<CompanyApplicationDto>> GetApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<CompanyApplicationDto>>(
            "api/platform/applications", cancellationToken) ?? [];
    }

    public async Task<CompanyApplicationApprovedDto> ApproveAsync(
        Guid applicationId,
        string approvedCompanySize,
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PostAsJsonAsync(
            $"api/platform/applications/{applicationId}/approve",
            new ApproveCompanyApplicationDto(approvedCompanySize, "Approved by Platform Admin."),
            cancellationToken);
        return await ReadResponseAsync<CompanyApplicationApprovedDto>(response, cancellationToken);
    }

    public async Task RejectAsync(Guid applicationId, string reason,
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        using var response = await Http.PostAsJsonAsync(
            $"api/platform/applications/{applicationId}/reject",
            new RejectCompanyApplicationDto(reason), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<PlatformAuditLogDto>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default)
    {
        ApiSession.ApplyAuthorization(Http);
        return await Http.GetFromJsonAsync<List<PlatformAuditLogDto>>(
            "api/platform/audit-logs?take=200", cancellationToken) ?? [];
    }

    private static async Task<T> ReadResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"The API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
    }
}

public sealed record SubmitCompanyApplicationDto(
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string RequestedCompanySize);

public sealed record CompanyApplicationSubmittedDto(Guid Id, string Status, DateTime SubmittedAtUtc);

public sealed record CompanyApplicationDto(
    Guid Id,
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string RequestedCompanySize,
    string Status,
    DateTime SubmittedAtUtc,
    string? TenantCode,
    string? ReviewNotes);

public sealed record ApproveCompanyApplicationDto(string? ApprovedCompanySize, string? ReviewNotes);
public sealed record RejectCompanyApplicationDto(string Reason);

public sealed record CompanyApplicationApprovedDto(
    Guid ApplicationId,
    string CompanyName,
    string TenantCode,
    string CompanySize,
    string TenantAdminEmail,
    string TenantAdminSetupToken,
    string ProvisioningStatus);

public sealed record PlatformAuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorEmail,
    string Tenant,
    string Action,
    string EntityType,
    string EntityId,
    string Summary,
    DateTime OccurredAtUtc);
