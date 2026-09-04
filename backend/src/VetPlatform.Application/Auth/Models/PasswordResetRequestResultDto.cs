namespace VetPlatform.Application.Auth.Models;

public class PasswordResetRequestResultDto
{
    public string Message { get; init; } = "Si el correo existe, enviaremos instrucciones para restablecer la contraseña.";
    public string? ResetUrl { get; init; }
}
