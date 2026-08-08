using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Patients.Models;

namespace VetPlatform.Application.Patients.Queries.GetPatients;

public class GetPatientsQueryHandler : IRequestHandler<GetPatientsQuery, IReadOnlyList<PatientDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPatientsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PatientDto>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Patients.AsNoTracking();

        if (request.OwnerId is { } ownerId)
        {
            query = query.Where(p => p.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(request.Species))
        {
            var species = request.Species.Trim();
            query = query.Where(p => p.Species == species);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Breed != null && p.Breed.Contains(search)) ||
                (p.MicrochipNumber != null && p.MicrochipNumber.Contains(search)) ||
                p.Owner!.FullName.Contains(search));
        }

        return await query
            .OrderBy(p => p.Name)
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
            .ToListAsync(cancellationToken);
    }
}
