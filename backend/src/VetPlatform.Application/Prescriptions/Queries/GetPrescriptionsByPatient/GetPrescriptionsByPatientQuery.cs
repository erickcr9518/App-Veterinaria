using MediatR;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Queries.GetPrescriptionsByPatient;

public record GetPrescriptionsByPatientQuery(Guid PatientId) : IRequest<IReadOnlyList<PrescriptionSummaryDto>>;
