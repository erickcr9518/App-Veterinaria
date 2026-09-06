using FluentValidation;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Commands.SetUserRoles;

public class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command)
            .Custom((command, context) =>
            {
                var roles = ResolveRoles(command);
                if (roles.Count == 0)
                {
                    context.AddFailure(nameof(command.Roles), "Selecciona al menos un rol.");
                    return;
                }

                var invalidRole = roles.FirstOrDefault(role => !RoleNames.All.Contains(role));
                if (invalidRole is not null)
                {
                    context.AddFailure(nameof(command.Roles), $"El rol debe ser uno de: {string.Join(", ", RoleNames.All)}.");
                    return;
                }

                if (roles.Contains(RoleNames.PlatformAdministrator) && roles.Count > 1)
                {
                    context.AddFailure(nameof(command.Roles), "Superadministrador no se puede combinar con roles de clinica.");
                }
            });
    }

    private static IReadOnlyList<string> ResolveRoles(SetUserRolesCommand command)
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
