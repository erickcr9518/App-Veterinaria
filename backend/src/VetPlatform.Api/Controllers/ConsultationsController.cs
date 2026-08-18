using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Consultations.Commands.AmendConsultation;
using VetPlatform.Application.Consultations.Commands.CreateConsultation;
using VetPlatform.Application.Consultations.Commands.FinalizeConsultation;
using VetPlatform.Application.Consultations.Commands.UpdateConsultation;
using VetPlatform.Application.Consultations.Models;
using VetPlatform.Application.Consultations.Queries.GetConsultationById;
using VetPlatform.Application.Consultations.Queries.GetConsultationsByPatient;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly ISender _sender;

    public ConsultationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.RecordsReadFull)]
    public async Task<ActionResult<ConsultationDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetConsultationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/patients/{patientId:guid}/consultations")]
    [Authorize(Policy = PermissionCodes.RecordsReadFull)]
    public async Task<ActionResult<IReadOnlyList<ConsultationSummaryDto>>> GetByPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetConsultationsByPatientQuery(patientId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ConsultationsWrite)]
    public async Task<ActionResult<Guid>> Create(CreateConsultationCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.ConsultationsWrite)]
    public async Task<IActionResult> Update(Guid id, UpdateConsultationRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateConsultationCommand(
            id,
            request.ReasonForVisit,
            request.HistoryOfPresentIllness,
            request.PhysicalExamFindings,
            request.TemperatureCelsius,
            request.HeartRateBpm,
            request.RespiratoryRateRpm,
            request.WeightKg,
            request.DiagnosticPlan,
            request.Treatment,
            request.Recommendations,
            request.FollowUpDate,
            request.Subjective,
            request.Objective,
            request.Assessment,
            request.Plan), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/finalize")]
    [Authorize(Policy = PermissionCodes.ConsultationsSign)]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new FinalizeConsultationCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/amend")]
    [Authorize(Policy = PermissionCodes.ConsultationsSign)]
    public async Task<IActionResult> Amend(Guid id, AmendConsultationRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AmendConsultationCommand(
            id,
            request.Reason,
            request.ReasonForVisit,
            request.HistoryOfPresentIllness,
            request.PhysicalExamFindings,
            request.TemperatureCelsius,
            request.HeartRateBpm,
            request.RespiratoryRateRpm,
            request.DiagnosticPlan,
            request.Treatment,
            request.Recommendations,
            request.FollowUpDate,
            request.Subjective,
            request.Objective,
            request.Assessment,
            request.Plan), cancellationToken);

        return NoContent();
    }
}

public record UpdateConsultationRequest(
    string ReasonForVisit,
    string? HistoryOfPresentIllness,
    string? PhysicalExamFindings,
    decimal? TemperatureCelsius,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    decimal? WeightKg,
    string? DiagnosticPlan,
    string? Treatment,
    string? Recommendations,
    DateOnly? FollowUpDate,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan);

public record AmendConsultationRequest(
    string Reason,
    string ReasonForVisit,
    string? HistoryOfPresentIllness,
    string? PhysicalExamFindings,
    decimal? TemperatureCelsius,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    string? DiagnosticPlan,
    string? Treatment,
    string? Recommendations,
    DateOnly? FollowUpDate,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan);
