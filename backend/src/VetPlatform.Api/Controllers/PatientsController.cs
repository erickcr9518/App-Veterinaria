using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Patients.Commands.CreatePatient;
using VetPlatform.Application.Patients.Commands.DeletePatient;
using VetPlatform.Application.Patients.Commands.UpdatePatient;
using VetPlatform.Application.Patients.Models;
using VetPlatform.Application.Patients.Queries.GetPatientById;
using VetPlatform.Application.Patients.Queries.GetPatients;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly ISender _sender;

    public PatientsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.PatientsRead)]
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? species,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPatientsQuery(search, ownerId, species), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PatientsRead)]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPatientByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PatientsWrite)]
    public async Task<ActionResult<Guid>> Create(CreatePatientCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PatientsWrite)]
    public async Task<IActionResult> Update(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdatePatientCommand(
            id,
            request.OwnerId,
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate,
            request.EstimatedAge,
            request.Sex,
            request.ReproductiveStatus,
            request.Color,
            request.CurrentWeightKg,
            request.MicrochipNumber,
            request.PhotoUrl,
            request.Allergies,
            request.ChronicDiseases,
            request.CurrentMedications,
            request.VaccinationStatus,
            request.DewormingStatus,
            request.Status), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PatientsWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeletePatientCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdatePatientRequest(
    Guid OwnerId,
    string Name,
    string Species,
    string? Breed,
    DateOnly? BirthDate,
    string? EstimatedAge,
    string Sex,
    string? ReproductiveStatus,
    string? Color,
    decimal? CurrentWeightKg,
    string? MicrochipNumber,
    string? PhotoUrl,
    string? Allergies,
    string? ChronicDiseases,
    string? CurrentMedications,
    string? VaccinationStatus,
    string? DewormingStatus,
    string Status);
