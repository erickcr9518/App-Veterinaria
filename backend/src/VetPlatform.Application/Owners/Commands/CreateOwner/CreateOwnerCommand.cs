using MediatR;

namespace VetPlatform.Application.Owners.Commands.CreateOwner;

public record CreateOwnerCommand(
    string FullName,
    string? IdentificationNumber,
    string Phone,
    string? Email,
    string? Address,
    string? AlternateContact,
    string? ConsentNotes) : IRequest<Guid>;
