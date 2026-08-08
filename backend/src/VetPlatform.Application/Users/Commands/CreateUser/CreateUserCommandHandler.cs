using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public CreateUserCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var isCreatingPlatformAdministrator = request.Role == RoleNames.PlatformAdministrator;

        if (isCreatingPlatformAdministrator && _currentUserService.Role != RoleNames.PlatformAdministrator)
        {
            throw new ForbiddenAccessException("Solo un superadministrador puede crear otros superadministradores.");
        }

        var clinicId = await ResolveClinicIdAsync(request, isCreatingPlatformAdministrator, cancellationToken);
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

    private async Task<Guid?> ResolveClinicIdAsync(
        CreateUserCommand request,
        bool isCreatingPlatformAdministrator,
        CancellationToken cancellationToken)
    {
        if (isCreatingPlatformAdministrator)
        {
            return null;
        }

        if (_currentUserService.Role == RoleNames.PlatformAdministrator)
        {
            var clinicId = request.ClinicId
                ?? throw new ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(nameof(request.ClinicId), "La clinica es requerida para crear usuarios de clinica.")
                });

            var clinicExists = await _dbContext.Clinics.AnyAsync(c => c.Id == clinicId, cancellationToken);
            if (!clinicExists)
            {
                throw new NotFoundException("Clinica", clinicId);
            }

            return clinicId;
        }

        return _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");
    }
}
