using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Prescriptions.Commands.CreatePrescription;

public class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreatePrescriptionCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");
        var veterinarianUserId = _currentUserService.UserId!.Value;

        var consultation = await _dbContext.Consultations
            .SingleOrDefaultAsync(c => c.Id == request.ConsultationId, cancellationToken)
            ?? throw new NotFoundException("Consulta", request.ConsultationId);

        var weight = request.WeightKgAtPrescription ?? consultation.WeightKg;

        var prescription = new Prescription
        {
            ClinicId = clinicId,
            PatientId = consultation.PatientId,
            ConsultationId = consultation.Id,
            VeterinarianUserId = veterinarianUserId,
            WeightKgAtPrescription = weight,
            GeneralInstructions = request.GeneralInstructions?.Trim(),
            Warnings = request.Warnings?.Trim(),
        };

        foreach (var item in request.Items)
        {
            prescription.Items.Add(new PrescriptionItem
            {
                ClinicId = clinicId,
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

        _dbContext.Prescriptions.Add(prescription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return prescription.Id;
    }
}
