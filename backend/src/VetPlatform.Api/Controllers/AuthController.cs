using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VetPlatform.Application.Auth.Commands.Login;
using VetPlatform.Application.Auth.Commands.Logout;
using VetPlatform.Application.Auth.Commands.RefreshToken;
using VetPlatform.Application.Auth.Commands.RequestPasswordReset;
using VetPlatform.Application.Auth.Commands.ResetPassword;
using VetPlatform.Application.Auth.Models;
using VetPlatform.Application.Common.Models;
using VetPlatform.Application.Auth.Queries.GetCurrentUser;

namespace VetPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        ISender sender,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _sender = sender;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Email, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<PasswordResetRequestResultDto>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var resetUrlBase = GetResetUrlBase();
        var token = await _sender.Send(
            new RequestPasswordResetCommand(request.Email, resetUrlBase),
            cancellationToken);

        return Ok(new PasswordResetRequestResultDto
        {
            ResetUrl = ShouldExposeResetUrl() && token is not null
                ? BuildResetUrl(resetUrlBase, token)
                : null,
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }

    private string GetResetUrlBase()
    {
        return _configuration["PasswordReset:ResetUrlBase"] ?? "http://localhost:4200/reset-password";
    }

    private bool ShouldExposeResetUrl()
    {
        return _environment.IsDevelopment()
            || _environment.IsEnvironment("Testing")
            || _configuration.GetValue("PasswordReset:ExposeResetUrlInResponse", false);
    }

    private static string BuildResetUrl(string resetUrlBase, PasswordResetToken token)
    {
        var separator = resetUrlBase.Contains('?') ? '&' : '?';
        return $"{resetUrlBase}{separator}email={Uri.EscapeDataString(token.Email)}&token={Uri.EscapeDataString(token.Token)}";
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
