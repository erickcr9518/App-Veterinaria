using MediatR;

namespace VetPlatform.Application.Patients.Commands.UpdatePatient;

public record UpdatePatientCommand(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Species,
    string? Breed,
    DateOnly? BirthDate,
    string? EstimatedAge,
    string Sex,
    string? ReproductiveStatus,
    string? Color,
    decimal? CurrentWeightKg,
    string? MicrochipNumber,
    string? PhotoUrl,
    string? Allergies,
    string? ChronicDiseases,
    string? CurrentMedications,
    string? VaccinationStatus,
    string? DewormingStatus,
    string Status) : IRequest;
