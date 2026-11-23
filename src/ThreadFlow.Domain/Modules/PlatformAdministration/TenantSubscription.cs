namespace ThreadFlow.Domain.Modules.PlatformAdministration;

public sealed class TenantSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Provisioning";
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAtUtc { get; set; }
}