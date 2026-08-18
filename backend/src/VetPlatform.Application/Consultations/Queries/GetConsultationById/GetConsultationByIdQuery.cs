using MediatR;
using VetPlatform.Application.Consultations.Models;

namespace VetPlatform.Application.Consultations.Queries.GetConsultationById;

public record GetConsultationByIdQuery(Guid Id) : IRequest<ConsultationDetailDto>;
