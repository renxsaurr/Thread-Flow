namespace ThreadFlow.Api.Authentication;

public sealed record CreateTenantUserResponse(
    string Id,
    string Email,
    string TenantCode,
    string Role,
    Guid? BranchId,
    string? BranchName,
    string SetupToken);
