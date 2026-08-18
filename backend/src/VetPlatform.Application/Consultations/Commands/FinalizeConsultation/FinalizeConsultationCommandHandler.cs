using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Consultations.Commands.FinalizeConsultation;

public class FinalizeConsultationCommandHandler : IRequestHandler<FinalizeConsultationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public FinalizeConsultationCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(FinalizeConsultationCommand request, CancellationToken cancellationToken)
    {
        var consultation = await _dbContext.Consultations
            .Include(c => c.SoapNote)
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Consulta", request.Id);

        if (consultation.Status != ConsultationStatus.Draft)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Id), "La consulta ya fue finalizada."),
            });
        }

        if (string.IsNullOrWhiteSpace(consultation.SoapNote?.Assessment) || string.IsNullOrWhiteSpace(consultation.SoapNote?.Plan))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(SoapNote.Assessment), "Completa la evaluacion y el plan antes de finalizar la consulta."),
            });
        }

        consultation.Status = ConsultationStatus.Finalized;
        consultation.FinalizedAtUtc = DateTime.UtcNow;
        consultation.FinalizedByUserId = _currentUserService.UserId;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
