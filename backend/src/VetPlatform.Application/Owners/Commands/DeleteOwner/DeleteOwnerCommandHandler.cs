using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Owners.Commands.DeleteOwner;

public class DeleteOwnerCommandHandler : IRequestHandler<DeleteOwnerCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteOwnerCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await _dbContext.Owners
            .Include(o => o.Patients)
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Propietario", request.Id);

        if (owner.Patients.Any())
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(nameof(request.Id), "No se puede eliminar un propietario con pacientes asociados.")
            });
        }

        _dbContext.Owners.Remove(owner);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
