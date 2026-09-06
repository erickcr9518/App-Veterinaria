using FluentValidation.Results;
using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Commands.SetUserRoles;

public class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public SetUserRolesCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var roles = ResolveRoles(request);
        if (roles.Count == 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Roles), "Selecciona al menos un rol."),
            });
        }

        if (request.UserId == _currentUserService.UserId)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.UserId), "No puedes cambiar los roles de tu propia cuenta."),
            });
        }

        var targetClinicId = await _identityService.GetUserClinicIdAsync(request.UserId);
        if (!_currentUserService.Roles.Contains(RoleNames.PlatformAdministrator))
        {
            var ownClinicId = _currentUserService.ClinicId
                ?? throw new ForbiddenAccessException("El usuario actual no está asociado a ninguna clínica.");

            if (targetClinicId != ownClinicId)
            {
                throw new NotFoundException("Usuario", request.UserId);
            }
        }
        else if (targetClinicId is null && !await _identityService.UserExistsAsync(request.UserId))
        {
            throw new NotFoundException("Usuario", request.UserId);
        }

        ValidateRolesMatchAccountScope(request, roles, targetClinicId);

        var updated = await _identityService.SetUserRolesAsync(request.UserId, roles);
        if (!updated.Succeeded)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Roles), string.Join(" ", updated.Errors)),
            });
        }
    }

    private static IReadOnlyList<string> ResolveRoles(SetUserRolesCommand request)
    {
        var roles = request.Roles is { Count: > 0 }
            ? request.Roles
            : string.IsNullOrWhiteSpace(request.Role)
                ? Array.Empty<string>()
                : new[] { request.Role };

        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateRolesMatchAccountScope(
        SetUserRolesCommand request,
        IReadOnlyList<string> roles,
        Guid? targetClinicId)
    {
        var isPlatformRole = roles.Contains(RoleNames.PlatformAdministrator);
        if (targetClinicId is null && !isPlatformRole)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Roles), "Las cuentas de plataforma solo pueden tener rol Superadministrador."),
            });
        }

        if (targetClinicId is not null && isPlatformRole)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Roles), "Superadministrador solo aplica a cuentas de plataforma."),
            });
        }
    }
}
