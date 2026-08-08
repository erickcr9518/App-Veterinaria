using MediatR;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new AuthenticationException("No hay una sesión activa.");

        var user = await _identityService.GetAuthenticatedUserAsync(userId)
            ?? throw new NotFoundException("Usuario", userId);

        return new CurrentUserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            ClinicId = user.ClinicId,
            ClinicName = user.ClinicName,
            Role = user.Role,
            Permissions = user.Permissions,
        };
    }
}
