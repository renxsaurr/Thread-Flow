namespace ThreadFlow.Domain.Modules.TenantAdministration;

/// <summary>Platform-level record used to locate a tenant's separate business databases.</summary>
public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanySize { get; set; } = "Micro";
    public bool IsActive { get; set; } = true;
    public string LocalDatabaseName { get; set; } = string.Empty;
    public string CloudDatabaseName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
