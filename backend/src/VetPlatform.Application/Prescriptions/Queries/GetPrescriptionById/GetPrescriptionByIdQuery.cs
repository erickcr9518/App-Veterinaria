using MediatR;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Queries.GetPrescriptionById;

public record GetPrescriptionByIdQuery(Guid Id) : IRequest<PrescriptionDetailDto>;
