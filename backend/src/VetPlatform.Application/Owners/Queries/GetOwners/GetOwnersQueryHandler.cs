using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Owners.Models;

namespace VetPlatform.Application.Owners.Queries.GetOwners;

public class GetOwnersQueryHandler : IRequestHandler<GetOwnersQuery, IReadOnlyList<OwnerDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetOwnersQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OwnerDto>> Handle(GetOwnersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Owners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(o =>
                o.FullName.Contains(search) ||
                (o.IdentificationNumber != null && o.IdentificationNumber.Contains(search)) ||
                o.Phone.Contains(search) ||
                (o.Email != null && o.Email.Contains(search)));
        }

        return await query
            .OrderBy(o => o.FullName)
            .Select(o => new OwnerDto
            {
                Id = o.Id,
                FullName = o.FullName,
                IdentificationNumber = o.IdentificationNumber,
                Phone = o.Phone,
                Email = o.Email,
                Address = o.Address,
                AlternateContact = o.AlternateContact,
                ConsentNotes = o.ConsentNotes,
                PatientCount = o.Patients.Count,
            })
            .ToListAsync(cancellationToken);
    }
}
