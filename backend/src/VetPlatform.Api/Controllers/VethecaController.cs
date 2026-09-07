using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Vetheca.Commands.SaveVethecaSearch;
using VetPlatform.Application.Vetheca.Commands.SubmitVethecaFeedback;
using VetPlatform.Application.Vetheca.Commands.UnsaveVethecaSearch;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Application.Vetheca.Queries.AskVetheca;
using VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearchById;
using VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearches;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

// Vetheca (see docs/VETIA_CLINIC_ANALYSIS.md, section J): PubMed search +
// LLM synthesis over the retrieved articles, plus saved searches. Not
// linked from the frontend nav yet - see the "Vetheca rollout" decision in
// that doc. Synthesis comes back null when Anthropic:ApiKey isn't
// configured yet; the raw articles are still returned either way. Every
// ask is logged as a VethecaSearchLog row (audit trail), regardless of
// whether the user ever calls Save on it.
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

    [HttpPost("{id:guid}/save")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<IActionResult> Save(Guid id, SaveVethecaSearchRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SaveVethecaSearchCommand(id, request.Title), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unsave")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<IActionResult> Unsave(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new UnsaveVethecaSearchCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/feedback")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<IActionResult> SubmitFeedback(Guid id, SubmitVethecaFeedbackRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SubmitVethecaFeedbackCommand(id, request.Helpful, request.Note), cancellationToken);
        return NoContent();
    }

    [HttpGet("saved")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<ActionResult<IReadOnlyList<VethecaSavedSearchSummaryDto>>> GetSaved(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSavedVethecaSearchesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("saved/{id:guid}")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<ActionResult<VethecaSavedSearchDetailDto>> GetSavedById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSavedVethecaSearchByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}

public record AskVethecaRequest(string Question, int? MaxResults);

public record SaveVethecaSearchRequest(string? Title);

public record SubmitVethecaFeedbackRequest(bool Helpful, string? Note);
