using FluentValidation;

namespace VetPlatform.Application.Consultations.Commands.AmendConsultation;

public class AmendConsultationCommandValidator : AbstractValidator<AmendConsultationCommand>
{
    public AmendConsultationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000)
            .WithMessage("Explica por que se corrige esta consulta finalizada.");
        RuleFor(x => x.ReasonForVisit).NotEmpty().MaximumLength(500);
        RuleFor(x => x.HistoryOfPresentIllness).MaximumLength(2000);
        RuleFor(x => x.PhysicalExamFindings).MaximumLength(2000);
        RuleFor(x => x.TemperatureCelsius).InclusiveBetween(20m, 45m).When(x => x.TemperatureCelsius.HasValue);
        RuleFor(x => x.HeartRateBpm).InclusiveBetween(20, 400).When(x => x.HeartRateBpm.HasValue);
        RuleFor(x => x.RespiratoryRateRpm).InclusiveBetween(5, 150).When(x => x.RespiratoryRateRpm.HasValue);
        RuleFor(x => x.DiagnosticPlan).MaximumLength(2000);
        RuleFor(x => x.Treatment).MaximumLength(2000);
        RuleFor(x => x.Recommendations).MaximumLength(2000);
        RuleFor(x => x.Subjective).MaximumLength(2000);
        RuleFor(x => x.Objective).MaximumLength(2000);
        RuleFor(x => x.Assessment).MaximumLength(2000);
        RuleFor(x => x.Plan).MaximumLength(2000);
    }
}
