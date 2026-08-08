using MediatR;

namespace VetPlatform.Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string Password,
    string FullName,
    string Role) : IRequest<Guid>;
