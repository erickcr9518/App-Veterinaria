using MediatR;

namespace VetPlatform.Application.Vetheca.Commands.SaveVethecaSearch;

public record SaveVethecaSearchCommand(Guid Id, string? Title) : IRequest;
