using MediatR;
using VetPlatform.Application.Consultations.Models;

namespace VetPlatform.Application.Consultations.Queries.GetConsultationsByPatient;

public record GetConsultationsByPatientQuery(Guid PatientId) : IRequest<IReadOnlyList<ConsultationSummaryDto>>;
