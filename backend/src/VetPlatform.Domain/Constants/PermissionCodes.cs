namespace VetPlatform.Domain.Constants;

public static class PermissionCodes
{
    public const string OwnersRead = "owners.read";
    public const string OwnersWrite = "owners.write";

    public const string PatientsRead = "patients.read";
    public const string PatientsWrite = "patients.write";

    public const string RecordsReadFull = "records.read.full";
    public const string RecordsReadBasic = "records.read.basic";

    public const string ConsultationsWrite = "consultations.write";
    public const string ConsultationsSign = "consultations.sign";

    public const string PrescriptionsWrite = "prescriptions.write";

    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsWrite = "appointments.write";
    public const string AppointmentsWriteOwn = "appointments.write.own";

    public const string UsersManage = "users.manage";
    public const string ClinicsManage = "clinics.manage";

    public const string AuditReadAll = "audit.read.all";
    public const string AuditReadOwn = "audit.read.own";

    public static readonly IReadOnlyList<(string Code, string Module, string Description)> Catalog = new[]
    {
        (OwnersRead, "Propietarios", "Ver propietarios"),
        (OwnersWrite, "Propietarios", "Crear y editar propietarios"),
        (PatientsRead, "Pacientes", "Ver pacientes"),
        (PatientsWrite, "Pacientes", "Crear y editar pacientes"),
        (RecordsReadFull, "Expediente", "Ver expediente clínico completo"),
        (RecordsReadBasic, "Expediente", "Ver solo datos básicos del paciente"),
        (ConsultationsWrite, "Consultas", "Registrar y editar consultas en borrador"),
        (ConsultationsSign, "Consultas", "Firmar y finalizar una consulta"),
        (PrescriptionsWrite, "Recetas", "Crear y firmar recetas"),
        (AppointmentsRead, "Agenda", "Ver la agenda"),
        (AppointmentsWrite, "Agenda", "Crear, editar y cancelar cualquier cita"),
        (AppointmentsWriteOwn, "Agenda", "Gestionar únicamente las citas propias"),
        (UsersManage, "Administración", "Gestionar usuarios, roles y configuración de la clínica"),
        (ClinicsManage, "Administración", "Crear y administrar clínicas"),
        (AuditReadAll, "Auditoría", "Ver la bitácora completa de la clínica"),
        (AuditReadOwn, "Auditoría", "Ver únicamente la bitácora de las propias acciones"),
    };
}
