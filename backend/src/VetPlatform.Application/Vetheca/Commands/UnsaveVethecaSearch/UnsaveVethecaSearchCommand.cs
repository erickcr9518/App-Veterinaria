using MediatR;

namespace VetPlatform.Application.Vetheca.Commands.UnsaveVethecaSearch;

public record UnsaveVethecaSearchCommand(Guid Id) : IRequest;
