using MediatR;
using VetPlatform.Application.Common.Models;

namespace VetPlatform.Application.Users.Queries.GetUsers;

public record GetUsersQuery(Guid? ClinicId = null) : IRequest<IReadOnlyList<UserSummary>>;
