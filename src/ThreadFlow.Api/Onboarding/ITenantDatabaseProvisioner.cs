namespace ThreadFlow.Api.Onboarding;

public interface ITenantDatabaseProvisioner
{
    Task ProvisionAsync(string databaseName, CancellationToken cancellationToken);
}