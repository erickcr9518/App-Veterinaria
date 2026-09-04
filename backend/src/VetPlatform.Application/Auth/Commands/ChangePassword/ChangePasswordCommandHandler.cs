using FluentValidation.Results;
using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new AuthenticationException("Debes iniciar sesión para cambiar tu contraseña.");

        var result = await _identityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.CurrentPassword), string.Join(" ", result.Errors)),
            });
        }
    }
}
