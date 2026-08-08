using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Owners.Commands.CreateOwner;
using VetPlatform.Application.Owners.Commands.DeleteOwner;
using VetPlatform.Application.Owners.Commands.UpdateOwner;
using VetPlatform.Application.Owners.Models;
using VetPlatform.Application.Owners.Queries.GetOwnerById;
using VetPlatform.Application.Owners.Queries.GetOwners;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/owners")]
[Authorize]
public class OwnersController : ControllerBase
{
    private readonly ISender _sender;

    public OwnersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.OwnersRead)]
    public async Task<ActionResult<IReadOnlyList<OwnerDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOwnersQuery(search), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.OwnersRead)]
    public async Task<ActionResult<OwnerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOwnerByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.OwnersWrite)]
    public async Task<ActionResult<Guid>> Create(CreateOwnerCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.OwnersWrite)]
    public async Task<IActionResult> Update(Guid id, UpdateOwnerRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateOwnerCommand(
            id,
            request.FullName,
            request.IdentificationNumber,
            request.Phone,
            request.Email,
            request.Address,
            request.AlternateContact,
            request.ConsentNotes), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.OwnersWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteOwnerCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdateOwnerRequest(
    string FullName,
    string? IdentificationNumber,
    string Phone,
    string? Email,
    string? Address,
    string? AlternateContact,
    string? ConsentNotes);
