using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;

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
        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no está asociado a ninguna clínica.");

        return _identityService.GetUsersByClinicAsync(clinicId);
    }
}
