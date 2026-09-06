using MediatR;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearches;

public record GetSavedVethecaSearchesQuery : IRequest<IReadOnlyList<VethecaSavedSearchSummaryDto>>;
