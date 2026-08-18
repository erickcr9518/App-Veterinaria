using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Consultations.Models;

namespace VetPlatform.Application.Consultations.Queries.GetConsultationsByPatient;

public class GetConsultationsByPatientQueryHandler : IRequestHandler<GetConsultationsByPatientQuery, IReadOnlyList<ConsultationSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetConsultationsByPatientQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<ConsultationSummaryDto>> Handle(GetConsultationsByPatientQuery request, CancellationToken cancellationToken)
    {
        var patientExists = await _dbContext.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            throw new NotFoundException("Paciente", request.PatientId);
        }

        var consultations = await _dbContext.Consultations
            .AsNoTracking()
            .Where(c => c.PatientId == request.PatientId)
            .OrderByDescending(c => c.ConsultationDateUtc)
            .ToListAsync(cancellationToken);

        var vetNames = await _identityService.GetUserFullNamesAsync(consultations.Select(c => c.VeterinarianUserId));

        return consultations
            .Select(c => new ConsultationSummaryDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                ConsultationDateUtc = c.ConsultationDateUtc,
                ReasonForVisit = c.ReasonForVisit,
                VeterinarianName = vetNames.GetValueOrDefault(c.VeterinarianUserId, "Veterinario"),
                Status = c.Status,
                FollowUpDate = c.FollowUpDate,
            })
            .ToList();
    }
}
