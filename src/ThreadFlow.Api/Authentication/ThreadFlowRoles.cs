namespace ThreadFlow.Api.Authentication;

public static class ThreadFlowRoles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantOwnerManager = "TenantOwnerManager";
    public const string BranchManager = "BranchManager";
    public const string SalesClerk = "SalesClerk";
    public const string CustomerServiceAgent = "CustomerServiceAgent";
    public const string InventoryPurchasingStaff = "InventoryPurchasingStaff";

    public static readonly IReadOnlyList<string> TenantAssignableEmployeeRoles =
    [
        SalesClerk,
        CustomerServiceAgent,
        InventoryPurchasingStaff,
        TenantOwnerManager,
        BranchManager
    ];

    public static bool RequiresBranchAssignment(string role) =>
        string.Equals(role, SalesClerk, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, CustomerServiceAgent, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, InventoryPurchasingStaff, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, BranchManager, StringComparison.OrdinalIgnoreCase);
}
