using MediatR;

namespace VetPlatform.Application.Consultations.Commands.AmendConsultation;

public record AmendConsultationCommand(
    Guid Id,
    string Reason,
    string ReasonForVisit,
    string? HistoryOfPresentIllness,
    string? PhysicalExamFindings,
    decimal? TemperatureCelsius,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    string? DiagnosticPlan,
    string? Treatment,
    string? Recommendations,
    DateOnly? FollowUpDate,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan) : IRequest;
