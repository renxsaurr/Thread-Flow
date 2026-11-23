namespace ThreadFlow.Domain.Modules.TenantAdministration;

/// <summary>
/// Logical local hosting group for tenant databases. Groups share the same tenant
/// schema and may be mapped to separate SQL Server instances in a future deployment.
/// </summary>
public sealed class TenantDatabaseGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanySize { get; set; } = string.Empty;
    public string LocalDatabasePrefix { get; set; } = string.Empty;
}
