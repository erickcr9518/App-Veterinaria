using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Prescriptions.Commands.FinalizePrescription;

public class FinalizePrescriptionCommandHandler : IRequestHandler<FinalizePrescriptionCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public FinalizePrescriptionCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(FinalizePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Receta", request.Id);

        if (prescription.Status != PrescriptionStatus.Draft)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Id), "La receta ya fue finalizada."),
            });
        }

        if (prescription.Items.Count == 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(prescription.Items), "Agrega al menos un producto antes de finalizar la receta."),
            });
        }

        prescription.Status = PrescriptionStatus.Finalized;
        prescription.FinalizedAtUtc = DateTime.UtcNow;
        prescription.FinalizedByUserId = _currentUserService.UserId;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
