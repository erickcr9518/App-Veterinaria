using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Patients.Commands.CreatePatient;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreatePatientCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");

        var ownerExists = await _dbContext.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken);
        if (!ownerExists)
        {
            throw new NotFoundException("Propietario", request.OwnerId);
        }

        var patient = new Patient
        {
            ClinicId = clinicId,
            OwnerId = request.OwnerId,
            Name = request.Name.Trim(),
            Species = request.Species.Trim(),
            Breed = request.Breed?.Trim(),
            BirthDate = request.BirthDate,
            EstimatedAge = request.EstimatedAge?.Trim(),
            Sex = request.Sex.Trim(),
            ReproductiveStatus = request.ReproductiveStatus?.Trim(),
            Color = request.Color?.Trim(),
            CurrentWeightKg = request.CurrentWeightKg,
            MicrochipNumber = request.MicrochipNumber?.Trim(),
            PhotoUrl = request.PhotoUrl?.Trim(),
            Allergies = request.Allergies?.Trim(),
            ChronicDiseases = request.ChronicDiseases?.Trim(),
            CurrentMedications = request.CurrentMedications?.Trim(),
            VaccinationStatus = request.VaccinationStatus?.Trim(),
            DewormingStatus = request.DewormingStatus?.Trim(),
            Status = request.Status.Trim(),
        };

        if (request.CurrentWeightKg is { } weight)
        {
            patient.WeightHistory.Add(new PatientWeight
            {
                ClinicId = clinicId,
                WeightKg = weight,
                RecordedAtUtc = DateTime.UtcNow,
                Notes = "Peso inicial",
            });
        }

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return patient.Id;
    }
}
