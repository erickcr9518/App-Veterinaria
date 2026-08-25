using FluentValidation.Results;
using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserSummary>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public GetUsersQueryHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<UserSummary>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role == RoleNames.PlatformAdministrator)
        {
            var clinicId = request.ClinicId
                ?? throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.ClinicId), "Selecciona una clinica para ver sus usuarios."),
                });

            return _identityService.GetUsersByClinicAsync(clinicId);
        }

        var ownClinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no está asociado a ninguna clínica.");

        return _identityService.GetUsersByClinicAsync(ownClinicId);
    }
}
