using MediatR;

namespace VetPlatform.Application.Vetheca.Commands.SubmitVethecaFeedback;

public record SubmitVethecaFeedbackCommand(Guid Id, bool Helpful, string? Note) : IRequest;
