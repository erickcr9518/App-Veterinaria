using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Appointments.Common;

internal static class AppointmentAccess
{
    public static Guid GetRequiredClinicId(ICurrentUserService currentUserService)
    {
        return currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");
    }

    public static Guid? ResolveAssignedVeterinarian(ICurrentUserService currentUserService, Guid? requestedVeterinarianUserId)
    {
        if (currentUserService.HasPermission(PermissionCodes.AppointmentsWrite))
        {
            return requestedVeterinarianUserId;
        }

        if (!currentUserService.HasPermission(PermissionCodes.AppointmentsWriteOwn))
        {
            throw new ForbiddenAccessException("No tienes permiso para gestionar citas.");
        }

        var currentUserId = currentUserService.UserId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        if (requestedVeterinarianUserId is { } requested && requested != currentUserId)
        {
            throw new ForbiddenAccessException("Solo puedes gestionar citas asignadas a ti.");
        }

        return currentUserId;
    }

    public static void EnsureCanManage(ICurrentUserService currentUserService, Guid? assignedVeterinarianUserId)
    {
        if (currentUserService.HasPermission(PermissionCodes.AppointmentsWrite))
        {
            return;
        }

        if (!currentUserService.HasPermission(PermissionCodes.AppointmentsWriteOwn))
        {
            throw new ForbiddenAccessException("No tienes permiso para gestionar citas.");
        }

        if (assignedVeterinarianUserId is null || assignedVeterinarianUserId != currentUserService.UserId)
        {
            throw new ForbiddenAccessException("Solo puedes gestionar citas asignadas a ti.");
        }
    }
}
