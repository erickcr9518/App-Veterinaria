namespace VetPlatform.Application.Common.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetAsync(string email, string fullName, string resetUrl, CancellationToken cancellationToken);
}
