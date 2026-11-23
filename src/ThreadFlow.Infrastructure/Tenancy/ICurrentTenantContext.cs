namespace ThreadFlow.Infrastructure.Tenancy;

/// <summary>Tenant database identity resolved for the current API request.</summary>
public interface ICurrentTenantContext
{
    Guid TenantId { get; }
    string Code { get; }
    string CompanySize { get; }
    string DatabaseName { get; }
    bool IsResolved { get; }

    void Set(Guid tenantId, string code, string companySize, string databaseName);
}
