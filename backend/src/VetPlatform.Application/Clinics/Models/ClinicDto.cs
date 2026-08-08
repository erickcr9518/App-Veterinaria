namespace VetPlatform.Application.Clinics.Models;

public class ClinicDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LegalId { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
