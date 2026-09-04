using MediatR;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;

namespace VetPlatform.Application.Auth.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, PasswordResetToken?>
{
    private readonly IIdentityService _identityService;
    private readonly IPasswordResetEmailSender _emailSender;

    public RequestPasswordResetCommandHandler(IIdentityService identityService, IPasswordResetEmailSender emailSender)
    {
        _identityService = identityService;
        _emailSender = emailSender;
    }

    public async Task<PasswordResetToken?> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var token = await _identityService.CreatePasswordResetTokenAsync(request.Email);
        if (token is null)
        {
            return null;
        }

        var resetUrl = BuildResetUrl(request.ResetUrlBase, token.Email, token.Token);
        await _emailSender.SendPasswordResetAsync(token.Email, token.FullName, resetUrl, cancellationToken);

        return token;
    }

    private static string BuildResetUrl(string resetUrlBase, string email, string token)
    {
        var separator = resetUrlBase.Contains('?') ? '&' : '?';
        return $"{resetUrlBase}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}
