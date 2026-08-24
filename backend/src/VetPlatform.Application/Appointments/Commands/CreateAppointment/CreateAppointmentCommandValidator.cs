using FluentValidation;

namespace VetPlatform.Application.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.StartsAtUtc).NotEmpty();
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc)
            .WithMessage("La hora final debe ser posterior a la hora inicial.");
        RuleFor(x => x.VisitType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.ReminderChannel).MaximumLength(50);
        RuleFor(x => x.ReminderNotes).MaximumLength(500);
    }
}
