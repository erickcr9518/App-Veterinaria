using MediatR;

namespace VetPlatform.Application.Patients.Commands.DeletePatient;

public record DeletePatientCommand(Guid Id) : IRequest;
