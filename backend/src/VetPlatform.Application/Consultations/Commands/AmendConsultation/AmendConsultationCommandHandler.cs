using System.Text.Json;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Consultations.Commands.AmendConsultation;

public class AmendConsultationCommandHandler : IRequestHandler<AmendConsultationCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public AmendConsultationCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AmendConsultationCommand request, CancellationToken cancellationToken)
    {
        var consultation = await _dbContext.Consultations
            .Include(c => c.SoapNote)
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Consulta", request.Id);

        if (consultation.Status != ConsultationStatus.Finalized)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Id), "Solo se pueden enmendar consultas ya finalizadas; edita el borrador directamente."),
            });
        }

        if (string.IsNullOrWhiteSpace(request.Assessment) || string.IsNullOrWhiteSpace(request.Plan))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(SoapNote.Assessment), "La evaluacion y el plan siguen siendo obligatorios en una consulta finalizada."),
            });
        }

        var previousValues = new
        {
            consultation.ReasonForVisit,
            consultation.HistoryOfPresentIllness,
            consultation.PhysicalExamFindings,
            consultation.TemperatureCelsius,
            consultation.HeartRateBpm,
            consultation.RespiratoryRateRpm,
            consultation.WeightKg,
            consultation.DiagnosticPlan,
            consultation.Treatment,
            consultation.Recommendations,
            consultation.FollowUpDate,
            Subjective = consultation.SoapNote?.Subjective,
            Objective = consultation.SoapNote?.Objective,
            Assessment = consultation.SoapNote?.Assessment,
            Plan = consultation.SoapNote?.Plan,
        };

        _dbContext.ConsultationAmendments.Add(new ConsultationAmendment
        {
            ClinicId = consultation.ClinicId,
            ConsultationId = consultation.Id,
            Reason = request.Reason.Trim(),
            PreviousValuesJson = JsonSerializer.Serialize(previousValues),
        });

        var previousWeight = consultation.WeightKg;

        consultation.ReasonForVisit = request.ReasonForVisit.Trim();
        consultation.HistoryOfPresentIllness = request.HistoryOfPresentIllness?.Trim();
        consultation.PhysicalExamFindings = request.PhysicalExamFindings?.Trim();
        consultation.TemperatureCelsius = request.TemperatureCelsius;
        consultation.HeartRateBpm = request.HeartRateBpm;
        consultation.RespiratoryRateRpm = request.RespiratoryRateRpm;
        consultation.WeightKg = request.WeightKg ?? consultation.WeightKg;
        consultation.DiagnosticPlan = request.DiagnosticPlan?.Trim();
        consultation.Treatment = request.Treatment?.Trim();
        consultation.Recommendations = request.Recommendations?.Trim();
        consultation.FollowUpDate = request.FollowUpDate;

        if (consultation.SoapNote is not null)
        {
            consultation.SoapNote.Subjective = request.Subjective?.Trim();
            consultation.SoapNote.Objective = request.Objective?.Trim();
            consultation.SoapNote.Assessment = request.Assessment?.Trim();
            consultation.SoapNote.Plan = request.Plan?.Trim();
        }

        if (request.WeightKg is { } newWeight && previousWeight != newWeight)
        {
            var patient = await _dbContext.Patients.SingleAsync(p => p.Id == consultation.PatientId, cancellationToken);
            patient.CurrentWeightKg = newWeight;
            _dbContext.PatientWeights.Add(new PatientWeight
            {
                ClinicId = consultation.ClinicId,
                PatientId = patient.Id,
                WeightKg = newWeight,
                RecordedAtUtc = DateTime.UtcNow,
                Notes = "Corregido mediante enmienda de consulta",
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
