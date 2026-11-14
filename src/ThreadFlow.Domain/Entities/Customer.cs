namespace ThreadFlow.Domain.Modules.Crm;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
