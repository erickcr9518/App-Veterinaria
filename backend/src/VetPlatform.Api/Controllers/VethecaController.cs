using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VetPlatform.Application.Vetheca.Commands.DeleteVethecaLibraryDocument;
using VetPlatform.Application.Vetheca.Commands.SaveVethecaSearch;
using VetPlatform.Application.Vetheca.Commands.SubmitVethecaFeedback;
using VetPlatform.Application.Vetheca.Commands.UnsaveVethecaSearch;
using VetPlatform.Application.Vetheca.Commands.UploadVethecaLibraryDocument;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Application.Vetheca.Queries.AskVetheca;
using VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearchById;
using VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearches;
using VetPlatform.Application.Vetheca.Queries.GetVethecaLibraryDocuments;
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
    [EnableRateLimiting("Vetheca")]
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

    // The clinic's own purchased reference literature (see the 2026-09-10
    // "bring your own literature" idea in docs/VETIA_CLINIC_ANALYSIS.md).
    // Shared across the clinic like Owners/Patients - any user with
    // vetheca.ask can upload, list, or remove a document, not just whoever
    // uploaded it.
    [HttpPost("library")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<VethecaLibraryDocumentDto>> UploadLibraryDocument(
        [FromForm] UploadVethecaLibraryDocumentRequest request, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);

        var result = await _sender.Send(
            new UploadVethecaLibraryDocumentCommand(request.Title, request.File.FileName, stream.ToArray()),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("library")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<ActionResult<IReadOnlyList<VethecaLibraryDocumentDto>>> GetLibraryDocuments(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVethecaLibraryDocumentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("library/{id:guid}")]
    [Authorize(Policy = PermissionCodes.VethecaAsk)]
    public async Task<IActionResult> DeleteLibraryDocument(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteVethecaLibraryDocumentCommand(id), cancellationToken);
        return NoContent();
    }
}

public record AskVethecaRequest(string Question, int? MaxResults);

public record SaveVethecaSearchRequest(string? Title);

public record SubmitVethecaFeedbackRequest(bool Helpful, string? Note);

public class UploadVethecaLibraryDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}
