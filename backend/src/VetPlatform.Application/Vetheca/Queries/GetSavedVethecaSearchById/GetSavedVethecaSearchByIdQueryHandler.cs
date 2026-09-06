using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearchById;

public class GetSavedVethecaSearchByIdQueryHandler : IRequestHandler<GetSavedVethecaSearchByIdQuery, VethecaSavedSearchDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetSavedVethecaSearchByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<VethecaSavedSearchDetailDto> Handle(GetSavedVethecaSearchByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _dbContext.VethecaSearchLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == request.Id && v.IsSaved, cancellationToken)
            ?? throw new NotFoundException("Búsqueda de Vetheca", request.Id);

        if (log.CreatedByUserId != _currentUserService.UserId)
        {
            throw new ForbiddenAccessException("Solo podés ver tus propias búsquedas guardadas de Vetheca.");
        }

        var stored = JsonSerializer.Deserialize<VethecaStoredResult>(log.ResultJson)
            ?? new VethecaStoredResult(Array.Empty<PubMedArticleDto>(), null);

        return new VethecaSavedSearchDetailDto
        {
            Id = log.Id,
            Question = log.Question,
            Title = log.Title,
            CreatedAtUtc = log.CreatedAtUtc,
            Articles = stored.Articles,
            Synthesis = stored.Synthesis,
        };
    }
}
