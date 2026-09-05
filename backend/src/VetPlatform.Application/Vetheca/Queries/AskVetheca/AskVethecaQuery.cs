using MediatR;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.AskVetheca;

public record AskVethecaQuery(string Question, int MaxResults = 5) : IRequest<IReadOnlyList<PubMedArticleDto>>;
