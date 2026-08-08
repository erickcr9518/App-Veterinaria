namespace VetPlatform.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? ClinicId { get; }
    string? Role { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permissionCode);
}
