using FluentValidation;

namespace VetPlatform.Application.Vetheca.Commands.SaveVethecaSearch;

public class SaveVethecaSearchCommandValidator : AbstractValidator<SaveVethecaSearchCommand>
{
    public SaveVethecaSearchCommandValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200);
    }
}
