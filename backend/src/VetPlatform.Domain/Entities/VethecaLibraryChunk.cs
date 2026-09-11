using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

// One searchable slice of a VethecaLibraryDocument's extracted text - usually
// one PDF page, split further only if a page is unusually long. ClinicId is
// denormalized from the parent document (same pattern as PrescriptionItem)
// so the tenant query filter still applies even when chunks are queried
// directly, without needing a join back to the document on every query.
public class VethecaLibraryChunk : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;

    public VethecaLibraryDocument? Document { get; set; }
}
