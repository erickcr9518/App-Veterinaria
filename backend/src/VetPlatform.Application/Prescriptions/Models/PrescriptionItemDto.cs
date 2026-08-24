namespace VetPlatform.Application.Prescriptions.Models;

public class PrescriptionItemDto
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Concentration { get; init; }
    public string? Presentation { get; init; }
    public string Quantity { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string? Instructions { get; init; }
}
