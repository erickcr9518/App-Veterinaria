using MediatR;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Commands.CreatePrescription;

public record CreatePrescriptionCommand(
    Guid ConsultationId,
    decimal? WeightKgAtPrescription,
    string? GeneralInstructions,
    string? Warnings,
    IReadOnlyList<PrescriptionItemInput> Items) : IRequest<Guid>;
