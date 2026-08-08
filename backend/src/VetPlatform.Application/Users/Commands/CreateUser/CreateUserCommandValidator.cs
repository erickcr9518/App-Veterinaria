using FluentValidation;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().Must(role => RoleNames.All.Contains(role))
            .WithMessage($"El rol debe ser uno de: {string.Join(", ", RoleNames.All)}");
    }
}
