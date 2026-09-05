using MediatR;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.AskVetheca;

// First slice of Vetheca (MVP step 1-2, see docs/VETIA_CLINIC_ANALYSIS.md section J):
// search PubMed and return raw articles. No LLM synthesis, no patient context,
// nothing persisted yet - this only validates the external integration.
public class AskVethecaQueryHandler : IRequestHandler<AskVethecaQuery, IReadOnlyList<PubMedArticleDto>>
{
    private readonly IPubMedClient _pubMedClient;

    public AskVethecaQueryHandler(IPubMedClient pubMedClient)
    {
        _pubMedClient = pubMedClient;
    }

    public Task<IReadOnlyList<PubMedArticleDto>> Handle(AskVethecaQuery request, CancellationToken cancellationToken)
    {
        return _pubMedClient.SearchAsync(request.Question, request.MaxResults, cancellationToken);
    }
}
