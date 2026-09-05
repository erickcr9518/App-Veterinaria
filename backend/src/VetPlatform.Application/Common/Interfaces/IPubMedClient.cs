using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Common.Interfaces;

public interface IPubMedClient
{
    Task<IReadOnlyList<PubMedArticleDto>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken);
}
