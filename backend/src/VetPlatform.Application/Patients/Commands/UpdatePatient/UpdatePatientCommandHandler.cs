using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Patients.Commands.UpdatePatient;

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdatePatientCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients.SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Paciente", request.Id);

        var ownerExists = await _dbContext.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken);
        if (!ownerExists)
        {
            throw new NotFoundException("Propietario", request.OwnerId);
        }

        var previousWeight = patient.CurrentWeightKg;

        patient.OwnerId = request.OwnerId;
        patient.Name = request.Name.Trim();
        patient.Species = request.Species.Trim();
        patient.Breed = request.Breed?.Trim();
        patient.BirthDate = request.BirthDate;
        patient.EstimatedAge = request.EstimatedAge?.Trim();
        patient.Sex = request.Sex.Trim();
        patient.ReproductiveStatus = request.ReproductiveStatus?.Trim();
        patient.Color = request.Color?.Trim();
        patient.CurrentWeightKg = request.CurrentWeightKg;
        patient.MicrochipNumber = request.MicrochipNumber?.Trim();
        patient.PhotoUrl = request.PhotoUrl?.Trim();
        patient.Allergies = request.Allergies?.Trim();
        patient.ChronicDiseases = request.ChronicDiseases?.Trim();
        patient.CurrentMedications = request.CurrentMedications?.Trim();
        patient.VaccinationStatus = request.VaccinationStatus?.Trim();
        patient.DewormingStatus = request.DewormingStatus?.Trim();
        patient.Status = request.Status.Trim();

        if (request.CurrentWeightKg is { } newWeight && previousWeight != newWeight)
        {
            _dbContext.PatientWeights.Add(new PatientWeight
            {
                ClinicId = patient.ClinicId,
                PatientId = patient.Id,
                WeightKg = newWeight,
                RecordedAtUtc = DateTime.UtcNow,
                Notes = "Actualizacion de peso",
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
