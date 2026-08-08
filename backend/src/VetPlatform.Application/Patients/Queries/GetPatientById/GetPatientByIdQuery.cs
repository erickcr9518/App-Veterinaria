using MediatR;
using VetPlatform.Application.Patients.Models;

namespace VetPlatform.Application.Patients.Queries.GetPatientById;

public record GetPatientByIdQuery(Guid Id) : IRequest<PatientDto>;
