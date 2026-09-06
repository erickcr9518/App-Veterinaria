using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Vetheca.Commands.UnsaveVethecaSearch;

// Un-saves a search - it stops showing up as one of the user's saved
// searches, but the underlying VethecaSearchLog row (and its audit trail
// value) is kept, never deleted. See VethecaSearchLog's own comment.
public class UnsaveVethecaSearchCommandHandler : IRequestHandler<UnsaveVethecaSearchCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UnsaveVethecaSearchCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UnsaveVethecaSearchCommand request, CancellationToken cancellationToken)
    {
        var log = await _dbContext.VethecaSearchLogs
            .SingleOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Búsqueda de Vetheca", request.Id);

        if (log.CreatedByUserId != _currentUserService.UserId)
        {
            throw new ForbiddenAccessException("Solo podés quitar tus propias búsquedas guardadas de Vetheca.");
        }

        log.IsSaved = false;
        log.Title = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
