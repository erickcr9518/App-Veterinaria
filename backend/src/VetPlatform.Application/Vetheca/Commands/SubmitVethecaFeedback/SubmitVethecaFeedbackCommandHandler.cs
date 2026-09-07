using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Vetheca.Commands.SubmitVethecaFeedback;

public class SubmitVethecaFeedbackCommandHandler : IRequestHandler<SubmitVethecaFeedbackCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SubmitVethecaFeedbackCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SubmitVethecaFeedbackCommand request, CancellationToken cancellationToken)
    {
        var log = await _dbContext.VethecaSearchLogs
            .SingleOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Búsqueda de Vetheca", request.Id);

        if (log.CreatedByUserId != _currentUserService.UserId)
        {
            throw new ForbiddenAccessException("Solo podés calificar tus propias consultas de Vetheca.");
        }

        log.Feedback = request.Helpful;
        log.FeedbackNote = request.Note?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
