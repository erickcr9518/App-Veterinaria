using MediatR;
using VetPlatform.Application.Auth.Models;

namespace VetPlatform.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<AuthResultDto>;
