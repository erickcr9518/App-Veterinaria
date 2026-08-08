using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Owners.Models;

namespace VetPlatform.Application.Owners.Queries.GetOwnerById;

public class GetOwnerByIdQueryHandler : IRequestHandler<GetOwnerByIdQuery, OwnerDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetOwnerByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OwnerDto> Handle(GetOwnerByIdQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Owners
            .AsNoTracking()
            .Where(o => o.Id == request.Id)
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
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Propietario", request.Id);
    }
}
