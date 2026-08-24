using FluentValidation;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Commands.UpdatePrescription;

public class UpdatePrescriptionCommandValidator : AbstractValidator<UpdatePrescriptionCommand>
{
    public UpdatePrescriptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WeightKgAtPrescription).GreaterThan(0).LessThan(500).When(x => x.WeightKgAtPrescription.HasValue);
        RuleFor(x => x.GeneralInstructions).MaximumLength(2000);
        RuleFor(x => x.Warnings).MaximumLength(1000);
        RuleForEach(x => x.Items).SetValidator(new PrescriptionItemInputValidator());
    }
}
