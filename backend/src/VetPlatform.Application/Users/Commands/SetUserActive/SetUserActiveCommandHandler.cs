using FluentValidation.Results;
using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Commands.SetUserActive;

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public SetUserActiveCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == _currentUserService.UserId)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.UserId), "No puedes desactivar tu propia cuenta."),
            });
        }

        if (_currentUserService.Role != RoleNames.PlatformAdministrator)
        {
            var ownClinicId = _currentUserService.ClinicId
                ?? throw new ForbiddenAccessException("El usuario actual no está asociado a ninguna clínica.");

            var targetClinicId = await _identityService.GetUserClinicIdAsync(request.UserId);
            if (targetClinicId != ownClinicId)
            {
                throw new NotFoundException("Usuario", request.UserId);
            }
        }

        var updated = await _identityService.SetUserActiveAsync(request.UserId, request.IsActive);
        if (!updated)
        {
            throw new NotFoundException("Usuario", request.UserId);
        }
    }
}
