using MediatR;

namespace VetPlatform.Application.Prescriptions.Commands.FinalizePrescription;

public record FinalizePrescriptionCommand(Guid Id) : IRequest;
