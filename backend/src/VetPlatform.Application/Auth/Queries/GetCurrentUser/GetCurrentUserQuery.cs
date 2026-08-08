using MediatR;
using VetPlatform.Application.Auth.Models;

namespace VetPlatform.Application.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
