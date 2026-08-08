namespace VetPlatform.Domain.Constants;

public static class RoleNames
{
    public const string PlatformAdministrator = "SuperAdministrador";
    public const string Administrator = "Administrador";
    public const string Veterinarian = "Veterinario";
    public const string Receptionist = "Recepcion";

    public static readonly IReadOnlyList<string> All = new[]
    {
        PlatformAdministrator, Administrator, Veterinarian, Receptionist
    };
}
