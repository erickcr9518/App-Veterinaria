using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Prescriptions.Models;

namespace VetPlatform.Application.Prescriptions.Queries.GetPrescriptionById;

public class GetPrescriptionByIdQueryHandler : IRequestHandler<GetPrescriptionByIdQuery, PrescriptionDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetPrescriptionByIdQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<PrescriptionDetailDto> Handle(GetPrescriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var prescription = await _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .Include(p => p.Patient)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Receta", request.Id);

        var userIds = new List<Guid> { prescription.VeterinarianUserId };
        if (prescription.FinalizedByUserId is { } finalizedBy)
        {
            userIds.Add(finalizedBy);
        }

        var names = await _identityService.GetUserFullNamesAsync(userIds);

        return new PrescriptionDetailDto
        {
            Id = prescription.Id,
            ConsultationId = prescription.ConsultationId,
            PatientId = prescription.PatientId,
            PatientName = prescription.Patient?.Name ?? string.Empty,
            VeterinarianName = names.GetValueOrDefault(prescription.VeterinarianUserId, "Veterinario"),
            IssuedAtUtc = prescription.IssuedAtUtc,
            WeightKgAtPrescription = prescription.WeightKgAtPrescription,
            GeneralInstructions = prescription.GeneralInstructions,
            Warnings = prescription.Warnings,
            Status = prescription.Status,
            FinalizedAtUtc = prescription.FinalizedAtUtc,
            FinalizedByName = prescription.FinalizedByUserId is { } fbId ? names.GetValueOrDefault(fbId, "Veterinario") : null,
            Items = prescription.Items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Concentration = i.Concentration,
                Presentation = i.Presentation,
                Quantity = i.Quantity,
                Route = i.Route,
                Frequency = i.Frequency,
                Duration = i.Duration,
                Instructions = i.Instructions,
            }).ToList(),
        };
    }
}
