using MediatR;

namespace VetPlatform.Application.Clinics.Commands.CreateClinic;

public record CreateClinicCommand(
    string Name,
    string? LegalId,
    string? Address,
    string? Phone,
    string? Email,
    string TimeZone) : IRequest<Guid>;
