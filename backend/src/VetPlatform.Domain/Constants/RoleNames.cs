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

    public static readonly IReadOnlyList<string> ClinicRoles = new[]
    {
        Administrator, Veterinarian, Receptionist
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
