using MediatR;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetVethecaQuota;

public record GetVethecaQuotaQuery : IRequest<VethecaQuotaDto>;
