using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Clinics.Models;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Clinics.Queries.GetClinics;

public class GetClinicsQueryHandler : IRequestHandler<GetClinicsQuery, IReadOnlyList<ClinicDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetClinicsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ClinicDto>> Handle(GetClinicsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Clinics.AsNoTracking().AsQueryable();

        if (_currentUserService.ClinicId is { } clinicId)
        {
            query = query.Where(c => c.Id == clinicId);
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ClinicDto
            {
                Id = c.Id,
                Name = c.Name,
                LegalId = c.LegalId,
                Address = c.Address,
                Phone = c.Phone,
                Email = c.Email,
                TimeZone = c.TimeZone,
                IsActive = c.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}
