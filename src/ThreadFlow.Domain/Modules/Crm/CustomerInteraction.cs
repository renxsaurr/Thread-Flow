namespace ThreadFlow.Domain.Modules.Crm;

public enum InteractionType
{
    Inquiry,
    Complaint,
    Feedback
}

public enum InteractionStatus
{
    Open,
    InProgress,
    Resolved
}

public sealed class CustomerInteraction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public InteractionType Type { get; set; }
    public InteractionStatus Status { get; set; } = InteractionStatus.Open;
    public string Subject { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
