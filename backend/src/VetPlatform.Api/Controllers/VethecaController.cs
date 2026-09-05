using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Vetheca.Queries.AskVetheca;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

// Vetheca (see docs/VETIA_CLINIC_ANALYSIS.md, section J): PubMed search +
// LLM synthesis over the retrieved articles. Not linked from the frontend
// nav yet - see the "Vetheca rollout" decision in that doc. Synthesis comes
// back null when Anthropic:ApiKey isn't configured yet; the raw articles
// are still returned either way.
[ApiController]
[Route("api/vetheca")]
[Authorize]
public class VethecaController : ControllerBase
{
    private readonly ISender _sender;

    public VethecaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("ask")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<ActionResult<AskVethecaResult>> Ask(AskVethecaRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AskVethecaQuery(request.Question, request.MaxResults ?? 5), cancellationToken);
        return Ok(result);
    }
}

public record AskVethecaRequest(string Question, int? MaxResults);
