namespace VetPlatform.Application.Common.Models;

public class PasswordResetToken
{
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}
