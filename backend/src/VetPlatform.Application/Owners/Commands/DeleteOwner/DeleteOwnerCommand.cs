using MediatR;

namespace VetPlatform.Application.Owners.Commands.DeleteOwner;

public record DeleteOwnerCommand(Guid Id) : IRequest;
