using MediatR;

namespace VetPlatform.Application.Owners.Commands.UpdateOwner;

public record UpdateOwnerCommand(
    Guid Id,
    string FullName,
    string? IdentificationNumber,
    string Phone,
    string? Email,
    string? Address,
    string? AlternateContact,
    string? ConsentNotes) : IRequest;
