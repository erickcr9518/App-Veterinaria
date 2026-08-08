using MediatR;
using VetPlatform.Application.Owners.Models;

namespace VetPlatform.Application.Owners.Queries.GetOwners;

public record GetOwnersQuery(string? Search) : IRequest<IReadOnlyList<OwnerDto>>;
