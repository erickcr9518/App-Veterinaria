using FluentValidation;

namespace VetPlatform.Application.Owners.Commands.CreateOwner;

public class CreateOwnerCommandValidator : AbstractValidator<CreateOwnerCommand>
{
    public CreateOwnerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IdentificationNumber).MaximumLength(50);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.AlternateContact).MaximumLength(200);
        RuleFor(x => x.ConsentNotes).MaximumLength(1000);
    }
}
