using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Common.Interfaces;

public interface IVethecaQuotaService
{
    Task<VethecaQuotaDto> GetStatusAsync(CancellationToken cancellationToken);

    // Throws QuotaExceededException when the current user has used up their
    // monthly Vetheca questions. No-op for users without a limit.
    Task EnsureAvailableAsync(CancellationToken cancellationToken);
}
