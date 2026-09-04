namespace VetPlatform.Application.Common.Models;

public class AuthenticatedUser
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string SecurityStamp { get; init; } = string.Empty;
    public Guid? ClinicId { get; init; }
    public string? ClinicName { get; init; }
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
