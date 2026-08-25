using MediatR;

namespace VetPlatform.Application.Users.Commands.SetUserActive;

public record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;
