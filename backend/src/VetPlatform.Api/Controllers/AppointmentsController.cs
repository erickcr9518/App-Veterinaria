using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Appointments.Commands.ChangeAppointmentStatus;
using VetPlatform.Application.Appointments.Commands.CreateAppointment;
using VetPlatform.Application.Appointments.Commands.UpdateAppointment;
using VetPlatform.Application.Appointments.Models;
using VetPlatform.Application.Appointments.Queries.GetAppointmentById;
using VetPlatform.Application.Appointments.Queries.GetAppointments;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AppointmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.AppointmentsRead)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAll(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? assignedVeterinarianUserId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var rangeStart = fromUtc ?? DateTime.UtcNow.Date;
        var rangeEnd = toUtc ?? rangeStart.AddDays(7);
        if (rangeEnd <= rangeStart)
        {
            return BadRequest("El rango de fechas de agenda no es valido.");
        }

        var result = await _sender.Send(new GetAppointmentsQuery(rangeStart, rangeEnd, patientId, assignedVeterinarianUserId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.AppointmentsRead)]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateAppointmentRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateAppointmentCommand(
            id,
            request.PatientId,
            request.AssignedVeterinarianUserId,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.VisitType,
            request.Reason,
            request.Notes,
            request.ReminderChannel,
            request.ReminderNotes), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeAppointmentStatusRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangeAppointmentStatusCommand(id, request.Status, request.Reason), cancellationToken);
        return NoContent();
    }
}

public record UpdateAppointmentRequest(
    Guid PatientId,
    Guid? AssignedVeterinarianUserId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string VisitType,
    string Reason,
    string? Notes,
    string? ReminderChannel,
    string? ReminderNotes);

public record ChangeAppointmentStatusRequest(string Status, string? Reason);
