using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Owners.Commands.UpdateOwner;

public class UpdateOwnerCommandHandler : IRequestHandler<UpdateOwnerCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateOwnerCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await _dbContext.Owners.SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Propietario", request.Id);

        owner.FullName = request.FullName.Trim();
        owner.IdentificationNumber = request.IdentificationNumber?.Trim();
        owner.Phone = request.Phone.Trim();
        owner.Email = request.Email?.Trim();
        owner.Address = request.Address?.Trim();
        owner.AlternateContact = request.AlternateContact?.Trim();
        owner.ConsentNotes = request.ConsentNotes?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
