using FluentValidation;

namespace VetPlatform.Application.Vetheca.Queries.AskVetheca;

public class AskVethecaQueryValidator : AbstractValidator<AskVethecaQuery>
{
    public AskVethecaQueryValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MaxResults).InclusiveBetween(1, 20);
    }
}
