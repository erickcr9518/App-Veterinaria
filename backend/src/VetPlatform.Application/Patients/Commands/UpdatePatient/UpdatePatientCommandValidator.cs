using FluentValidation;
using VetPlatform.Application.Patients.Commands.CreatePatient;

namespace VetPlatform.Application.Patients.Commands.UpdatePatient;

public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    private static readonly string[] Species = { "Perro", "Gato" };
    private static readonly string[] Statuses = { "Activo", "Fallecido", "Perdido", "Transferido" };

    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Species).NotEmpty().Must(x => Species.Contains(x)).WithMessage("La especie debe ser Perro o Gato.");
        RuleFor(x => x.Breed).MaximumLength(120);
        RuleFor(x => x.EstimatedAge).MaximumLength(80);
        RuleFor(x => x.Sex).NotEmpty().MaximumLength(30);
        RuleFor(x => x.ReproductiveStatus).MaximumLength(80);
        RuleFor(x => x.Color).MaximumLength(80);
        RuleFor(x => x.CurrentWeightKg).GreaterThan(0).LessThan(500).When(x => x.CurrentWeightKg.HasValue);
        RuleFor(x => x.MicrochipNumber).MaximumLength(80);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.Allergies).MaximumLength(1000);
        RuleFor(x => x.ChronicDiseases).MaximumLength(1000);
        RuleFor(x => x.CurrentMedications).MaximumLength(1000);
        RuleFor(x => x.VaccinationStatus).MaximumLength(500);
        RuleFor(x => x.DewormingStatus).MaximumLength(500);
        RuleFor(x => x.Status).NotEmpty().Must(x => Statuses.Contains(x)).WithMessage("Estado de paciente invalido.");
    }
}
