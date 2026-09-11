using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Common.Interfaces;

public interface ILibraryChunkSearchService
{
    // Ranks the clinic's own uploaded library chunks by keyword overlap with
    // query and returns the top maxResults. Empty when the clinic has no
    // library documents, or none of them mention anything relevant.
    Task<IReadOnlyList<LibraryChunkMatchDto>> SearchAsync(Guid clinicId, string query, int maxResults, CancellationToken cancellationToken);
}
