namespace VetPlatform.Application.Owners.Models;

public class OwnerDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? IdentificationNumber { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? AlternateContact { get; init; }
    public string? ConsentNotes { get; init; }
    public int PatientCount { get; init; }
}
