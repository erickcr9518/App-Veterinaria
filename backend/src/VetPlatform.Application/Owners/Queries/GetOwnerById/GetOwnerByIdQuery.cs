using MediatR;
using VetPlatform.Application.Owners.Models;

namespace VetPlatform.Application.Owners.Queries.GetOwnerById;

public record GetOwnerByIdQuery(Guid Id) : IRequest<OwnerDto>;
