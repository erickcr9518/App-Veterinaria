using FluentValidation;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Appointments.Commands.ChangeAppointmentStatus;

public class ChangeAppointmentStatusCommandValidator : AbstractValidator<ChangeAppointmentStatusCommand>
{
    public ChangeAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).Must(status => AppointmentStatus.All.Contains(status))
            .WithMessage("El estado de la cita no es valido.");
        RuleFor(x => x.Reason).NotEmpty().When(x => x.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
            .WithMessage("Indica la razon para cancelar o marcar como no asistio.");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
