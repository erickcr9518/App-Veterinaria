using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Queries.GetPrescriptionsByPatient;

public class GetPrescriptionsByPatientQueryHandler : IRequestHandler<GetPrescriptionsByPatientQuery, IReadOnlyList<PrescriptionSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetPrescriptionsByPatientQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<PrescriptionSummaryDto>> Handle(GetPrescriptionsByPatientQuery request, CancellationToken cancellationToken)
    {
        var patientExists = await _dbContext.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            throw new NotFoundException("Paciente", request.PatientId);
        }

        var prescriptions = await _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.PatientId == request.PatientId)
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
