using MediatR;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Commands.UpdatePrescription;

public record UpdatePrescriptionCommand(
    Guid Id,
    decimal? WeightKgAtPrescription,
    string? GeneralInstructions,
    string? Warnings,
    IReadOnlyList<PrescriptionItemInput> Items) : IRequest;
