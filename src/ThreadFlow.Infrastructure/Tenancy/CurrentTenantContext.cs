namespace ThreadFlow.Infrastructure.Tenancy;

public sealed class CurrentTenantContext : ICurrentTenantContext
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string CompanySize { get; private set; } = string.Empty;
    public string DatabaseName { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public void Set(Guid tenantId, string code, string companySize, string databaseName)
    {
        TenantId = tenantId;
        Code = code;
        CompanySize = companySize;
        DatabaseName = databaseName;
        IsResolved = true;
    }
}
