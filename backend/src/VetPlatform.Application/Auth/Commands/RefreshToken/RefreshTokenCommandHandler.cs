using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RefreshTokenCommandHandler(IApplicationDbContext dbContext, IIdentityService identityService, IJwtTokenGenerator tokenGenerator)
    {
        _dbContext = dbContext;
        _identityService = identityService;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken)
            ?? throw new AuthenticationException("Token de renovación inválido.");

        if (!existingToken.IsActive)
        {
            throw new AuthenticationException("El token de renovación expiró o fue revocado. Inicia sesión nuevamente.");
        }

        var user = await _identityService.GetAuthenticatedUserAsync(existingToken.UserId)
            ?? throw new AuthenticationException("El usuario asociado a este token ya no existe o está inactivo.");

        var newRefreshToken = _tokenGenerator.GenerateRefreshToken(user.UserId, request.IpAddress);

        await RemoveInactiveRefreshTokensAsync(user.UserId, request.RefreshToken, cancellationToken);

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken.Token;
        _dbContext.RefreshTokens.Add(newRefreshToken);

        var accessToken = _tokenGenerator.GenerateAccessToken(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResultDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAtUtc = newRefreshToken.ExpiresAtUtc,
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            ClinicId = user.ClinicId,
            ClinicName = user.ClinicName,
            Role = user.Role,
            Permissions = user.Permissions,
        };
    }

    private async Task RemoveInactiveRefreshTokensAsync(Guid userId, string currentToken, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var inactiveTokens = await _dbContext.RefreshTokens
            .Where(t =>
                t.UserId == userId &&
                t.Token != currentToken &&
                (t.RevokedAtUtc != null || t.ExpiresAtUtc <= now))
            .ToListAsync(cancellationToken);

        _dbContext.RefreshTokens.RemoveRange(inactiveTokens);
    }
}
