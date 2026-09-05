using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IApplicationDbContext _dbContext;

    public LoginCommandHandler(IIdentityService identityService, IJwtTokenGenerator tokenGenerator, IApplicationDbContext dbContext)
    {
        _identityService = identityService;
        _tokenGenerator = tokenGenerator;
        _dbContext = dbContext;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password)
            ?? throw new AuthenticationException("Correo o contraseña incorrectos.");

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken(user.UserId, request.IpAddress);

        await RemoveInactiveRefreshTokensAsync(user.UserId, cancellationToken);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResultDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            ClinicId = user.ClinicId,
            ClinicName = user.ClinicName,
            Role = user.Role,
            Permissions = user.Permissions,
        };
    }

    private async Task RemoveInactiveRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var inactiveTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && (t.RevokedAtUtc != null || t.ExpiresAtUtc <= now))
            .ToListAsync(cancellationToken);

        _dbContext.RefreshTokens.RemoveRange(inactiveTokens);
    }
}
