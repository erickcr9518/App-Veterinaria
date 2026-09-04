using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    public const string UserIdClaimType = "user_id";
    public const string ClinicIdClaimType = "clinic_id";
    public const string PermissionClaimType = "permission";
    public const string SecurityStampClaimType = "security_stamp";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Read HttpContext.User fresh on every access rather than caching it in
    // the constructor. This service can be constructed transitively before
    // authentication finishes (e.g. the JWT bearer's OnTokenValidated event
    // resolves UserManager, which resolves ApplicationDbContext for its
    // tenant query filter, which depends on this service) - caching would
    // permanently capture the pre-authentication anonymous principal.
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(UserIdClaimType)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? ClinicId
    {
        get
        {
            var value = User?.FindFirstValue(ClinicIdClaimType);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public IReadOnlyList<string> Permissions =>
        User?.FindAll(PermissionClaimType).Select(c => c.Value).ToList() ?? new List<string>();

    public bool HasPermission(string permissionCode) => Permissions.Contains(permissionCode);
}
