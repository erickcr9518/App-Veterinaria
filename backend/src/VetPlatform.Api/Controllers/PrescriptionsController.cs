using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Prescriptions.Commands.CreatePrescription;
using VetPlatform.Application.Prescriptions.Commands.FinalizePrescription;
using VetPlatform.Application.Prescriptions.Commands.UpdatePrescription;
using VetPlatform.Application.Prescriptions.Models;
using VetPlatform.Application.Prescriptions.Queries.GetPrescriptionById;
using VetPlatform.Application.Prescriptions.Queries.GetPrescriptionsByConsultation;
using VetPlatform.Application.Prescriptions.Queries.GetPrescriptionsByPatient;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public PrescriptionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.RecordsReadFull)]
    public async Task<ActionResult<PrescriptionDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPrescriptionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/patients/{patientId:guid}/prescriptions")]
    [Authorize(Policy = PermissionCodes.RecordsReadFull)]
    public async Task<ActionResult<IReadOnlyList<PrescriptionSummaryDto>>> GetByPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPrescriptionsByPatientQuery(patientId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/consultations/{consultationId:guid}/prescriptions")]
    [Authorize(Policy = PermissionCodes.RecordsReadFull)]
    public async Task<ActionResult<IReadOnlyList<PrescriptionSummaryDto>>> GetByConsultation(Guid consultationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPrescriptionsByConsultationQuery(consultationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PrescriptionsWrite)]
    public async Task<ActionResult<Guid>> Create(CreatePrescriptionCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PrescriptionsWrite)]
    public async Task<IActionResult> Update(Guid id, UpdatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdatePrescriptionCommand(
            id,
            request.WeightKgAtPrescription,
            request.GeneralInstructions,
            request.Warnings,
            request.Items), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/finalize")]
    [Authorize(Policy = PermissionCodes.PrescriptionsWrite)]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new FinalizePrescriptionCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdatePrescriptionRequest(
    decimal? WeightKgAtPrescription,
    string? GeneralInstructions,
    string? Warnings,
    IReadOnlyList<PrescriptionItemInput> Items);
