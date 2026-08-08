using VetPlatform.Application.Common.Models;

namespace VetPlatform.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password);

    Task<AuthenticatedUser?> GetAuthenticatedUserAsync(Guid userId);

    Task<UserAccountResult> CreateUserAsync(string email, string password, string fullName, Guid clinicId, string role);

    Task<IReadOnlyList<UserSummary>> GetUsersByClinicAsync(Guid clinicId);

    Task<bool> SetUserActiveAsync(Guid userId, bool isActive);
}
