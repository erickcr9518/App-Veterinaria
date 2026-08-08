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
        if (request.Role == RoleNames.PlatformAdministrator &&
            _currentUserService.Role != RoleNames.PlatformAdministrator)
        {
            throw new ForbiddenAccessException("Solo un superadministrador puede crear otros superadministradores.");
        }

        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no está asociado a ninguna clínica.");

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
