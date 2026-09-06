using MediatR;

namespace VetPlatform.Application.Users.Commands.SetUserRoles;

public record SetUserRolesCommand(Guid UserId, string? Role = null, IReadOnlyList<string>? Roles = null) : IRequest;
