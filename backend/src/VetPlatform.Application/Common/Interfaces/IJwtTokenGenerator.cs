using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(AuthenticatedUser user);

    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress);
}
