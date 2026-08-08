using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public CreateUserCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var isCreatingPlatformAdministrator = request.Role == RoleNames.PlatformAdministrator;

        if (isCreatingPlatformAdministrator && _currentUserService.Role != RoleNames.PlatformAdministrator)
        {
            throw new ForbiddenAccessException("Solo un superadministrador puede crear otros superadministradores.");
        }

        // Los superadministradores no pertenecen a ninguna clínica; el resto de roles hereda
        // la clínica de quien los crea, así queda garantizado que no puedan asignarse a otra.
        Guid? clinicId = isCreatingPlatformAdministrator
            ? null
            : _currentUserService.ClinicId ?? throw new ForbiddenAccessException("El usuario actual no está asociado a ninguna clínica.");

        var result = await _identityService.CreateUserAsync(request.Email, request.Password, request.FullName, clinicId, request.Role);

        if (!result.Succeeded)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), string.Join(" ", result.Errors))
            });
        }

        return result.UserId!.Value;
    }
}
