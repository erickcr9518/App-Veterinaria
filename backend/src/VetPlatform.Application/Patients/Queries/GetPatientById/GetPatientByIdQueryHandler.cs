using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Patients.Models;

namespace VetPlatform.Application.Patients.Queries.GetPatientById;

public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, PatientDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPatientByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientDto> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Patients
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                OwnerName = p.Owner!.FullName,
                Name = p.Name,
                Species = p.Species,
                Breed = p.Breed,
                BirthDate = p.BirthDate,
                EstimatedAge = p.EstimatedAge,
                Sex = p.Sex,
                ReproductiveStatus = p.ReproductiveStatus,
                Color = p.Color,
                CurrentWeightKg = p.CurrentWeightKg,
                MicrochipNumber = p.MicrochipNumber,
                PhotoUrl = p.PhotoUrl,
                Allergies = p.Allergies,
                ChronicDiseases = p.ChronicDiseases,
                CurrentMedications = p.CurrentMedications,
                VaccinationStatus = p.VaccinationStatus,
                DewormingStatus = p.DewormingStatus,
                Status = p.Status,
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Paciente", request.Id);
    }
}
