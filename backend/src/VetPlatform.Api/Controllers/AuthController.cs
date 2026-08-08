using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Auth.Commands.Login;
using VetPlatform.Application.Auth.Commands.RefreshToken;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Auth.Queries.GetCurrentUser;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Email, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
