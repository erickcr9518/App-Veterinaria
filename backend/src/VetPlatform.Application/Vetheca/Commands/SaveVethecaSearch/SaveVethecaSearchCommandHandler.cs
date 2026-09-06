using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Vetheca.Commands.SaveVethecaSearch;

public class SaveVethecaSearchCommandHandler : IRequestHandler<SaveVethecaSearchCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SaveVethecaSearchCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SaveVethecaSearchCommand request, CancellationToken cancellationToken)
    {
        var log = await _dbContext.VethecaSearchLogs
            .SingleOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Búsqueda de Vetheca", request.Id);

        if (log.CreatedByUserId != _currentUserService.UserId)
        {
            throw new ForbiddenAccessException("Solo podés guardar tus propias búsquedas de Vetheca.");
        }

        log.IsSaved = true;
        log.Title = request.Title?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
