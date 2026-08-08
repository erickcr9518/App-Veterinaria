using MediatR;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Clinics.Commands.CreateClinic;

public class CreateClinicCommandHandler : IRequestHandler<CreateClinicCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateClinicCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
    {
        var clinic = new Clinic
        {
            Name = request.Name,
            LegalId = request.LegalId,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            TimeZone = request.TimeZone,
        };

        _dbContext.Clinics.Add(clinic);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return clinic.Id;
    }
}
