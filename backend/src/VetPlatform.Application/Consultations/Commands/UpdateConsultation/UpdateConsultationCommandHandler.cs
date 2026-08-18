using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Consultations.Commands.UpdateConsultation;

public class UpdateConsultationCommandHandler : IRequestHandler<UpdateConsultationCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateConsultationCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateConsultationCommand request, CancellationToken cancellationToken)
    {
        var consultation = await _dbContext.Consultations
            .Include(c => c.SoapNote)
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Consulta", request.Id);

        if (consultation.Status != ConsultationStatus.Draft)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Id), "La consulta ya fue finalizada; use el endpoint de enmienda para corregirla."),
            });
        }

        var patient = await _dbContext.Patients.SingleAsync(p => p.Id == consultation.PatientId, cancellationToken);
        var previousWeight = consultation.WeightKg;

        consultation.ReasonForVisit = request.ReasonForVisit.Trim();
        consultation.HistoryOfPresentIllness = request.HistoryOfPresentIllness?.Trim();
        consultation.PhysicalExamFindings = request.PhysicalExamFindings?.Trim();
        consultation.TemperatureCelsius = request.TemperatureCelsius;
        consultation.HeartRateBpm = request.HeartRateBpm;
        consultation.RespiratoryRateRpm = request.RespiratoryRateRpm;
        consultation.WeightKg = request.WeightKg;
        consultation.DiagnosticPlan = request.DiagnosticPlan?.Trim();
        consultation.Treatment = request.Treatment?.Trim();
        consultation.Recommendations = request.Recommendations?.Trim();
        consultation.FollowUpDate = request.FollowUpDate;

        if (consultation.SoapNote is null)
        {
            consultation.SoapNote = new SoapNote { ClinicId = consultation.ClinicId };
        }

        consultation.SoapNote.Subjective = request.Subjective?.Trim();
        consultation.SoapNote.Objective = request.Objective?.Trim();
        consultation.SoapNote.Assessment = request.Assessment?.Trim();
        consultation.SoapNote.Plan = request.Plan?.Trim();

        if (request.WeightKg is { } newWeight && previousWeight != newWeight)
        {
            patient.CurrentWeightKg = newWeight;
            _dbContext.PatientWeights.Add(new PatientWeight
            {
                ClinicId = consultation.ClinicId,
                PatientId = patient.Id,
                WeightKg = newWeight,
                RecordedAtUtc = DateTime.UtcNow,
                Notes = "Actualizado durante consulta",
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
