using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Owners.Commands.CreateOwner;

public class CreateOwnerCommandHandler : IRequestHandler<CreateOwnerCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateOwnerCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
    {
        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");

        var owner = new Owner
        {
            ClinicId = clinicId,
            FullName = request.FullName.Trim(),
            IdentificationNumber = request.IdentificationNumber?.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            AlternateContact = request.AlternateContact?.Trim(),
            ConsentNotes = request.ConsentNotes?.Trim(),
        };

        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return owner.Id;
    }
}
