using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public AccessTokenResult GenerateAccessToken(AuthenticatedUser user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(CurrentUserService.UserIdClaimType, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(CurrentUserService.SecurityStampClaimType, user.SecurityStamp),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.ClinicId is { } clinicId)
        {
            claims.Add(new Claim(CurrentUserService.ClinicIdClaimType, clinicId.ToString()));
        }

        claims.AddRange(user.Permissions.Select(p => new Claim(CurrentUserService.PermissionClaimType, p)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress,
        };
    }
}
