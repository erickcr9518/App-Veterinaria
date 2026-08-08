using MediatR;
using VetPlatform.Application.Patients.Models;

namespace VetPlatform.Application.Patients.Queries.GetPatients;

public record GetPatientsQuery(string? Search, Guid? OwnerId, string? Species) : IRequest<IReadOnlyList<PatientDto>>;
