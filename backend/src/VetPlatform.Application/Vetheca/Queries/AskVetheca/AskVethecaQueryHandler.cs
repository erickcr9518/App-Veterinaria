using MediatR;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Vetheca.Queries.AskVetheca;

// Vetheca MVP (see docs/VETIA_CLINIC_ANALYSIS.md section J):
// step 1-2 search PubMed for raw articles; step 3 (this) adds an LLM
// synthesis over exactly those articles, with citations. Still nothing
// persisted, no patient context - deliberately kept stateless until the
// shape/quality of the synthesis is validated with real use.
public class AskVethecaQueryHandler : IRequestHandler<AskVethecaQuery, AskVethecaResult>
{
    private readonly IPubMedClient _pubMedClient;
    private readonly ILlmClient _llmClient;

    public AskVethecaQueryHandler(IPubMedClient pubMedClient, ILlmClient llmClient)
    {
        _pubMedClient = pubMedClient;
        _llmClient = llmClient;
    }

    public async Task<AskVethecaResult> Handle(AskVethecaQuery request, CancellationToken cancellationToken)
    {
        var articles = await _pubMedClient.SearchAsync(request.Question, request.MaxResults, cancellationToken);

        if (articles.Count == 0)
        {
            return new AskVethecaResult(articles, Synthesis: null);
        }

        var synthesis = await _llmClient.SynthesizeAsync(request.Question, articles, cancellationToken);
        return new AskVethecaResult(articles, synthesis);
    }
}
