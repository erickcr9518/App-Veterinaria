using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Consultations.Commands.CreateConsultation;

public class CreateConsultationCommandHandler : IRequestHandler<CreateConsultationCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateConsultationCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateConsultationCommand request, CancellationToken cancellationToken)
    {
        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");
        var veterinarianUserId = _currentUserService.UserId!.Value;

        var patient = await _dbContext.Patients.SingleOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new NotFoundException("Paciente", request.PatientId);

        var consultation = new Consultation
        {
            ClinicId = clinicId,
            PatientId = patient.Id,
            VeterinarianUserId = veterinarianUserId,
            ReasonForVisit = request.ReasonForVisit.Trim(),
            HistoryOfPresentIllness = request.HistoryOfPresentIllness?.Trim(),
            PhysicalExamFindings = request.PhysicalExamFindings?.Trim(),
            TemperatureCelsius = request.TemperatureCelsius,
            HeartRateBpm = request.HeartRateBpm,
            RespiratoryRateRpm = request.RespiratoryRateRpm,
            WeightKg = request.WeightKg,
            DiagnosticPlan = request.DiagnosticPlan?.Trim(),
            Treatment = request.Treatment?.Trim(),
            Recommendations = request.Recommendations?.Trim(),
            FollowUpDate = request.FollowUpDate,
        };

        consultation.SoapNote = new SoapNote
        {
            ClinicId = clinicId,
            Subjective = request.Subjective?.Trim(),
            Objective = request.Objective?.Trim(),
            Assessment = request.Assessment?.Trim(),
            Plan = request.Plan?.Trim(),
        };

        if (request.WeightKg is { } weight)
        {
            patient.CurrentWeightKg = weight;
            _dbContext.PatientWeights.Add(new PatientWeight
            {
                ClinicId = clinicId,
                PatientId = patient.Id,
                WeightKg = weight,
                RecordedAtUtc = DateTime.UtcNow,
                Notes = "Registrado durante consulta",
            });
        }

        _dbContext.Consultations.Add(consultation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return consultation.Id;
    }
}
