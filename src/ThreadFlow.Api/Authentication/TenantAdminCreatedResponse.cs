namespace ThreadFlow.Api.Authentication;

public sealed record TenantAdminCreatedResponse(
    string Id,
    string Email,
    string TenantCode,
    string Role);
