using MediatR;
using VetPlatform.Application.Auth.Models;

namespace VetPlatform.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password, string? IpAddress) : IRequest<AuthResultDto>;
