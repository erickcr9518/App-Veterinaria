using FluentValidation;

namespace VetPlatform.Application.Vetheca.Commands.SubmitVethecaFeedback;

public class SubmitVethecaFeedbackCommandValidator : AbstractValidator<SubmitVethecaFeedbackCommand>
{
    public SubmitVethecaFeedbackCommandValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
