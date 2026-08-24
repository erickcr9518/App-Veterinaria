using FluentValidation;

namespace VetPlatform.Application.Prescriptions.Models;

public class PrescriptionItemInputValidator : AbstractValidator<PrescriptionItemInput>
{
    public PrescriptionItemInputValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Concentration).MaximumLength(100);
        RuleFor(x => x.Presentation).MaximumLength(100);
        RuleFor(x => x.Quantity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Route).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Duration).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Instructions).MaximumLength(500);
    }
}
