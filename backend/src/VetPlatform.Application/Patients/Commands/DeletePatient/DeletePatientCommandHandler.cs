using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Patients.Commands.DeletePatient;

public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public DeletePatientCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients.SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Paciente", request.Id);

        _dbContext.Patients.Remove(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
