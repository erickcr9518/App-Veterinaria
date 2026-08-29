using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;
using VetPlatform.Infrastructure.Persistence;

namespace VetPlatform.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        if (await _userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        return await BuildAuthenticatedUserAsync(user);
    }

    public async Task<AuthenticatedUser?> GetAuthenticatedUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return await BuildAuthenticatedUserAsync(user);
    }

    public async Task<UserAccountResult> CreateUserAsync(string email, string password, string fullName, Guid? clinicId, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            ClinicId = clinicId,
            IsActive = true,
            LockoutEnabled = true,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return UserAccountResult.Failure(createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return UserAccountResult.Failure(roleResult.Errors.Select(e => e.Description));
        }

        return UserAccountResult.Success(user.Id);
    }

    public async Task<IReadOnlyList<UserSummary>> GetUsersByClinicAsync(Guid clinicId)
    {
        var users = await _userManager.Users
            .Where(u => u.ClinicId == clinicId)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var summaries = new List<UserSummary>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            summaries.Add(new UserSummary
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
            });
        }

        return summaries;
    }

    public async Task<Guid?> GetUserClinicIdAsync(Guid userId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user?.ClinicId;
    }

    public async Task<bool> SetUserActiveAsync(Guid userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserFullNamesAsync(IEnumerable<Guid> userIds)
    {
        var distinctIds = userIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _userManager.Users
            .Where(u => distinctIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
    }

    public async Task<bool> UserBelongsToClinicAsync(Guid userId, Guid clinicId)
    {
        return await _userManager.Users
            .AnyAsync(u => u.Id == userId && u.ClinicId == clinicId && u.IsActive);
    }

    private async Task<AuthenticatedUser> BuildAuthenticatedUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        var permissions = Array.Empty<string>();
        if (!string.IsNullOrEmpty(role))
        {
            var appRole = await _roleManager.FindByNameAsync(role);
            if (appRole is not null)
            {
                permissions = await _dbContext.RolePermissions
                    .Where(rp => rp.RoleId == appRole.Id)
                    .Select(rp => rp.Permission.Code)
                    .ToArrayAsync();
            }
        }

        string? clinicName = null;
        if (user.ClinicId is { } clinicId)
        {
            clinicName = await _dbContext.Clinics
                .Where(c => c.Id == clinicId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        return new AuthenticatedUser
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            ClinicId = user.ClinicId,
            ClinicName = clinicName,
            Role = role,
            Permissions = permissions,
        };
    }
}
