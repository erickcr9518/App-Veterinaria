namespace VetPlatform.Application.Vetheca.Models;

// The shape persisted as VethecaSearchLog.ResultJson - deliberately separate
// from AskVethecaResult (which also carries the log entry's Id, not part of
// what actually needs storing).
public record VethecaStoredResult(IReadOnlyList<PubMedArticleDto> Articles, VethecaSynthesisDto? Synthesis);
