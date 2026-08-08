namespace VetPlatform.Domain.Constants;

public static class RoleDefaultPermissions
{
    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        [RoleNames.PlatformAdministrator] = new[]
        {
            PermissionCodes.OwnersRead, PermissionCodes.OwnersWrite,
            PermissionCodes.PatientsRead, PermissionCodes.PatientsWrite,
            PermissionCodes.RecordsReadFull,
            PermissionCodes.AppointmentsRead, PermissionCodes.AppointmentsWrite,
            PermissionCodes.UsersManage, PermissionCodes.ClinicsManage,
            PermissionCodes.AuditReadAll,
        },
        [RoleNames.Administrator] = new[]
        {
            PermissionCodes.OwnersRead, PermissionCodes.OwnersWrite,
            PermissionCodes.PatientsRead, PermissionCodes.PatientsWrite,
            PermissionCodes.RecordsReadFull,
            PermissionCodes.AppointmentsRead, PermissionCodes.AppointmentsWrite,
            PermissionCodes.UsersManage,
            PermissionCodes.AuditReadAll,
        },
        [RoleNames.Veterinarian] = new[]
        {
            PermissionCodes.OwnersRead, PermissionCodes.OwnersWrite,
            PermissionCodes.PatientsRead, PermissionCodes.PatientsWrite,
            PermissionCodes.RecordsReadFull,
            PermissionCodes.ConsultationsWrite, PermissionCodes.ConsultationsSign,
            PermissionCodes.PrescriptionsWrite,
            PermissionCodes.AppointmentsRead, PermissionCodes.AppointmentsWriteOwn,
            PermissionCodes.AuditReadOwn,
        },
        [RoleNames.Receptionist] = new[]
        {
            PermissionCodes.OwnersRead, PermissionCodes.OwnersWrite,
            PermissionCodes.PatientsRead, PermissionCodes.PatientsWrite,
            PermissionCodes.RecordsReadBasic,
            PermissionCodes.AppointmentsRead, PermissionCodes.AppointmentsWrite,
        },
    };
}
