using MediatR;

namespace VetPlatform.Application.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
