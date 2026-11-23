using Microsoft.AspNetCore.Identity;

namespace ThreadFlow.Infrastructure.data.Master;

/// <summary>
/// A platform operator has no tenant assignment. Every company user is assigned
/// to exactly one tenant; their Identity roles are scoped to that tenant.
/// </summary>
public sealed class PlatformUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsPlatformAdmin { get; set; }
}
