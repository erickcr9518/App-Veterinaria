using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetSavedVethecaSearches;

public class GetSavedVethecaSearchesQueryHandler : IRequestHandler<GetSavedVethecaSearchesQuery, IReadOnlyList<VethecaSavedSearchSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetSavedVethecaSearchesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<VethecaSavedSearchSummaryDto>> Handle(GetSavedVethecaSearchesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        return await _dbContext.VethecaSearchLogs
            .AsNoTracking()
            .Where(v => v.IsSaved && v.CreatedByUserId == userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new VethecaSavedSearchSummaryDto
            {
                Id = v.Id,
                Question = v.Question,
                Title = v.Title,
                ArticleCount = v.ArticleCount,
                EvidenceSufficient = v.EvidenceSufficient,
                CreatedAtUtc = v.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
