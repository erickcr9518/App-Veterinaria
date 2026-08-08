using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Clinics.Commands.CreateClinic;
using VetPlatform.Application.Clinics.Models;
using VetPlatform.Application.Clinics.Queries.GetClinics;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/clinics")]
[Authorize]
public class ClinicsController : ControllerBase
{
    private readonly ISender _sender;

    public ClinicsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClinicDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetClinicsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ClinicsManage)]
    public async Task<ActionResult<Guid>> Create(CreateClinicCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }
}
