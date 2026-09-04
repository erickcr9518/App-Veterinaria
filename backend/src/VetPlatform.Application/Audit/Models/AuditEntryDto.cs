namespace VetPlatform.Application.Audit.Models;

public class AuditEntryDto
{
    public Guid Id { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string PerformedByName { get; init; } = string.Empty;
}
