using MediatR;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetVethecaQuota;

public class GetVethecaQuotaQueryHandler : IRequestHandler<GetVethecaQuotaQuery, VethecaQuotaDto>
{
    private readonly IVethecaQuotaService _quotaService;

    public GetVethecaQuotaQueryHandler(IVethecaQuotaService quotaService)
    {
        _quotaService = quotaService;
    }

    public Task<VethecaQuotaDto> Handle(GetVethecaQuotaQuery request, CancellationToken cancellationToken)
        => _quotaService.GetStatusAsync(cancellationToken);
}
