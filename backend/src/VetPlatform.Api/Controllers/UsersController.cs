using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Common.Models;
using VetPlatform.Application.Users.Commands.CreateUser;
using VetPlatform.Application.Users.Commands.SetUserActive;
using VetPlatform.Application.Users.Commands.SetUserRoles;
using VetPlatform.Application.Users.Queries.GetUsers;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = PermissionCodes.UsersManage)]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> GetAll([FromQuery] Guid? clinicId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUsersQuery(clinicId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetActive(Guid id, SetUserActiveRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetUserActiveCommand(id, request.IsActive), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetUserRolesCommand(id, request.Role, request.Roles), cancellationToken);
        return NoContent();
    }
}

public record SetUserActiveRequest(bool IsActive);

public record SetUserRolesRequest(string? Role = null, IReadOnlyList<string>? Roles = null);
