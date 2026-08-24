using MediatR;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Queries.GetPrescriptionsByConsultation;

public record GetPrescriptionsByConsultationQuery(Guid ConsultationId) : IRequest<IReadOnlyList<PrescriptionSummaryDto>>;
