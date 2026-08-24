using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Prescriptions.Commands.UpdatePrescription;

public class UpdatePrescriptionCommandHandler : IRequestHandler<UpdatePrescriptionCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdatePrescriptionCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Receta", request.Id);

        if (prescription.Status != PrescriptionStatus.Draft)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Id), "La receta ya fue finalizada y no se puede editar; genera una nueva."),
            });
        }

        prescription.WeightKgAtPrescription = request.WeightKgAtPrescription;
        prescription.GeneralInstructions = request.GeneralInstructions?.Trim();
        prescription.Warnings = request.Warnings?.Trim();

        // PrescriptionItem.PrescriptionId is required with cascade delete, so clearing the
        // navigation collection is enough for EF Core to mark the orphaned children Deleted.
        prescription.Items.Clear();

        foreach (var item in request.Items)
        {
            // Add directly to the DbSet (not to the already-tracked parent's navigation
            // collection): on an existing, tracked parent, adding new children through the
            // navigation left them picked up as Modified instead of Added, so EF Core sent an
            // UPDATE with a zeroed-out RowVersion and it never matched a row.
            _dbContext.PrescriptionItems.Add(new PrescriptionItem
            {
                ClinicId = prescription.ClinicId,
                PrescriptionId = prescription.Id,
                ProductName = item.ProductName.Trim(),
                Concentration = item.Concentration?.Trim(),
                Presentation = item.Presentation?.Trim(),
                Quantity = item.Quantity.Trim(),
                Route = item.Route.Trim(),
                Frequency = item.Frequency.Trim(),
                Duration = item.Duration.Trim(),
                Instructions = item.Instructions?.Trim(),
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
