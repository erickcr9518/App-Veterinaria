using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    public const string ClinicIdClaimType = "clinic_id";
    public const string PermissionClaimType = "permission";

    private readonly ClaimsPrincipal? _user;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;
    }

    public Guid? UserId
    {
        get
        {
            var value = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? ClinicId
    {
        get
        {
            var value = _user?.FindFirstValue(ClinicIdClaimType);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => _user?.FindFirstValue(ClaimTypes.Role);

    public IReadOnlyList<string> Permissions =>
        _user?.FindAll(PermissionClaimType).Select(c => c.Value).ToList() ?? new List<string>();

    public bool HasPermission(string permissionCode) => Permissions.Contains(permissionCode);
}
