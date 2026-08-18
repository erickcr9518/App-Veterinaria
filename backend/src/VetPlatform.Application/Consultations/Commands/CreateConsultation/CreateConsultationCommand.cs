using MediatR;

namespace VetPlatform.Application.Consultations.Commands.CreateConsultation;

public record CreateConsultationCommand(
    Guid PatientId,
    string ReasonForVisit,
    string? HistoryOfPresentIllness,
    string? PhysicalExamFindings,
    decimal? TemperatureCelsius,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    decimal? WeightKg,
    string? DiagnosticPlan,
    string? Treatment,
    string? Recommendations,
    DateOnly? FollowUpDate,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan) : IRequest<Guid>;
