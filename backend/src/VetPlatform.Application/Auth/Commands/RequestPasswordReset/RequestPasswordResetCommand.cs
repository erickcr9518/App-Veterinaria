using MediatR;
using VetPlatform.Application.Common.Models;

namespace VetPlatform.Application.Auth.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email, string ResetUrlBase) : IRequest<PasswordResetToken?>;
