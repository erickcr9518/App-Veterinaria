using MediatR;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearchById;

public record GetSavedVethecaSearchByIdQuery(Guid Id) : IRequest<VethecaSavedSearchDetailDto>;
