using MediatR;

namespace VetPlatform.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
