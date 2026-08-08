using MediatR;
using VetPlatform.Application.Clinics.Models;

namespace VetPlatform.Application.Clinics.Queries.GetClinics;

public record GetClinicsQuery : IRequest<IReadOnlyList<ClinicDto>>;
