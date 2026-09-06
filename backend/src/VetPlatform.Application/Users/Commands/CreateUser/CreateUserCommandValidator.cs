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
        RuleFor(x => x).Custom((command, context) =>
        {
            var roles = ResolveRoles(command);

            if (roles.Count == 0)
            {
                context.AddFailure(nameof(command.Roles), "Selecciona al menos un rol.");
                return;
            }

            var invalidRoles = roles.Where(role => !RoleNames.All.Contains(role)).ToArray();
            if (invalidRoles.Length > 0)
            {
                context.AddFailure(nameof(command.Roles), $"El rol debe ser uno de: {string.Join(", ", RoleNames.All)}");
                return;
            }

            if (roles.Contains(RoleNames.PlatformAdministrator) && roles.Count > 1)
            {
                context.AddFailure(nameof(command.Roles), "Superadministrador no se puede combinar con roles de clinica.");
            }
        });
    }

    private static IReadOnlyList<string> ResolveRoles(CreateUserCommand command)
    {
        var roles = command.Roles is { Count: > 0 }
            ? command.Roles
            : string.IsNullOrWhiteSpace(command.Role)
                ? Array.Empty<string>()
                : new[] { command.Role };

        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
