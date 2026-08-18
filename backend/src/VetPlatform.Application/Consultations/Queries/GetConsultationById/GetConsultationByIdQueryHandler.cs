using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Consultations.Models;

namespace VetPlatform.Application.Consultations.Queries.GetConsultationById;

public class GetConsultationByIdQueryHandler : IRequestHandler<GetConsultationByIdQuery, ConsultationDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetConsultationByIdQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<ConsultationDetailDto> Handle(GetConsultationByIdQuery request, CancellationToken cancellationToken)
    {
        var consultation = await _dbContext.Consultations
            .AsNoTracking()
            .Include(c => c.SoapNote)
            .Include(c => c.Patient)
            .Include(c => c.Amendments.OrderByDescending(a => a.CreatedAtUtc))
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Consulta", request.Id);

        var userIds = new List<Guid> { consultation.VeterinarianUserId };
        if (consultation.FinalizedByUserId is { } finalizedBy)
        {
            userIds.Add(finalizedBy);
        }
        userIds.AddRange(consultation.Amendments.Select(a => a.CreatedByUserId).OfType<Guid>());

        var names = await _identityService.GetUserFullNamesAsync(userIds);

        return new ConsultationDetailDto
        {
            Id = consultation.Id,
            PatientId = consultation.PatientId,
            PatientName = consultation.Patient?.Name ?? string.Empty,
            VeterinarianUserId = consultation.VeterinarianUserId,
            VeterinarianName = names.GetValueOrDefault(consultation.VeterinarianUserId, "Veterinario"),
            ConsultationDateUtc = consultation.ConsultationDateUtc,
            ReasonForVisit = consultation.ReasonForVisit,
            HistoryOfPresentIllness = consultation.HistoryOfPresentIllness,
            PhysicalExamFindings = consultation.PhysicalExamFindings,
            TemperatureCelsius = consultation.TemperatureCelsius,
            HeartRateBpm = consultation.HeartRateBpm,
            RespiratoryRateRpm = consultation.RespiratoryRateRpm,
            WeightKg = consultation.WeightKg,
            DiagnosticPlan = consultation.DiagnosticPlan,
            Treatment = consultation.Treatment,
            Recommendations = consultation.Recommendations,
            FollowUpDate = consultation.FollowUpDate,
            Status = consultation.Status,
            FinalizedAtUtc = consultation.FinalizedAtUtc,
            FinalizedByName = consultation.FinalizedByUserId is { } fbId ? names.GetValueOrDefault(fbId, "Veterinario") : null,
            Subjective = consultation.SoapNote?.Subjective,
            Objective = consultation.SoapNote?.Objective,
            Assessment = consultation.SoapNote?.Assessment,
            Plan = consultation.SoapNote?.Plan,
            Amendments = consultation.Amendments.Select(a => new ConsultationAmendmentDto
            {
                Id = a.Id,
                Reason = a.Reason,
                PreviousValuesJson = a.PreviousValuesJson,
                CreatedAtUtc = a.CreatedAtUtc,
                CreatedByName = a.CreatedByUserId is { } createdBy ? names.GetValueOrDefault(createdBy, "Veterinario") : "Veterinario",
            }).ToList(),
        };
    }
}
