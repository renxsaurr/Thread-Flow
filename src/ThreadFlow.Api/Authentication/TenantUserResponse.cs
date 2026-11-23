namespace ThreadFlow.Api.Authentication;

public sealed record TenantUserResponse(
    string Id,
    string Email,
    string TenantCode,
    IReadOnlyList<string> Roles,
    bool InvitationPending,
    Guid? BranchId,
    string? BranchName);
