using MediatR;

namespace VetPlatform.Application.Consultations.Commands.FinalizeConsultation;

public record FinalizeConsultationCommand(Guid Id) : IRequest;
