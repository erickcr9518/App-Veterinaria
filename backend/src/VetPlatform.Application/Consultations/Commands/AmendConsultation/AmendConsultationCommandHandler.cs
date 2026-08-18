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

        var previousValues = new
        {
            consultation.ReasonForVisit,
            consultation.HistoryOfPresentIllness,
            consultation.PhysicalExamFindings,
            consultation.TemperatureCelsius,
            consultation.HeartRateBpm,
            consultation.RespiratoryRateRpm,
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

        consultation.ReasonForVisit = request.ReasonForVisit.Trim();
        consultation.HistoryOfPresentIllness = request.HistoryOfPresentIllness?.Trim();
        consultation.PhysicalExamFindings = request.PhysicalExamFindings?.Trim();
        consultation.TemperatureCelsius = request.TemperatureCelsius;
        consultation.HeartRateBpm = request.HeartRateBpm;
        consultation.RespiratoryRateRpm = request.RespiratoryRateRpm;
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

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
