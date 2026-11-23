namespace ThreadFlow.Domain.Modules.PlatformAdministration;

public sealed class CompanyApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string RequestedCompanySize { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid? TenantId { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
}