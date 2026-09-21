namespace VetPlatform.Domain.Constants;

public static class RoleNames
{
    public const string PlatformAdministrator = "SuperAdministrador";
    public const string Administrator = "Administrador";
    public const string Veterinarian = "Veterinario";
    public const string Receptionist = "Recepcion";

    // A veterinarian who only uses Vetheca (evidence search) and none of the
    // clinical-management modules - the access level for pilot colleagues and,
    // later, self-registered users. Subject to a monthly question quota.
    public const string VethecaVeterinarian = "Veterinario Vetheca";

    public static readonly IReadOnlyList<string> All = new[]
    {
        PlatformAdministrator, Administrator, Veterinarian, Receptionist, VethecaVeterinarian
    };

    public static readonly IReadOnlyList<string> ClinicRoles = new[]
    {
        Administrator, Veterinarian, Receptionist, VethecaVeterinarian
    };

    public static string GetPrimaryRole(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.Ordinal);
        if (roleSet.Contains(PlatformAdministrator))
        {
            return PlatformAdministrator;
        }

        foreach (var role in ClinicRoles)
        {
            if (roleSet.Contains(role))
            {
                return role;
            }
        }

        return string.Empty;
    }
}
