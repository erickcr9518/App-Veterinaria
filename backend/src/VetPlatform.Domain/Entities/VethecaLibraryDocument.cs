using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

// A clinic's own purchased reference literature (a manual, textbook, journal
// PDF Erick or a vet legally bought), uploaded so Vetheca can search it
// alongside PubMed. Shared per-clinic like Owners/Patients, not private to
// the uploader - any staff member with vetheca.ask can use and manage it.
// Only the extracted text is stored (as VethecaLibraryChunk rows), never the
// original PDF bytes - keeps the system from holding a redistributable copy
// of content someone else owns the copyright to; it's the uploading clinic's
// responsibility that what they upload is theirs to use this way.
public class VethecaLibraryDocument : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PageCount { get; set; }

    public ICollection<VethecaLibraryChunk> Chunks { get; set; } = new List<VethecaLibraryChunk>();
}
