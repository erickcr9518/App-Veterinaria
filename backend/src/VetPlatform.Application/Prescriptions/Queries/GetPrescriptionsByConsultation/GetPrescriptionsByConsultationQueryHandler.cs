using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Queries.GetPrescriptionsByConsultation;

public class GetPrescriptionsByConsultationQueryHandler : IRequestHandler<GetPrescriptionsByConsultationQuery, IReadOnlyList<PrescriptionSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetPrescriptionsByConsultationQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<PrescriptionSummaryDto>> Handle(GetPrescriptionsByConsultationQuery request, CancellationToken cancellationToken)
    {
        var consultationExists = await _dbContext.Consultations.AnyAsync(c => c.Id == request.ConsultationId, cancellationToken);
        if (!consultationExists)
        {
            throw new NotFoundException("Consulta", request.ConsultationId);
        }

        var prescriptions = await _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.ConsultationId == request.ConsultationId)
            .OrderByDescending(p => p.IssuedAtUtc)
            .ToListAsync(cancellationToken);

        var vetNames = await _identityService.GetUserFullNamesAsync(prescriptions.Select(p => p.VeterinarianUserId));

        return prescriptions
            .Select(p => new PrescriptionSummaryDto
            {
                Id = p.Id,
                ConsultationId = p.ConsultationId,
                PatientId = p.PatientId,
                IssuedAtUtc = p.IssuedAtUtc,
                VeterinarianName = vetNames.GetValueOrDefault(p.VeterinarianUserId, "Veterinario"),
                Status = p.Status,
                ProductNames = p.Items.Select(i => i.ProductName).ToList(),
            })
            .ToList();
    }
}
