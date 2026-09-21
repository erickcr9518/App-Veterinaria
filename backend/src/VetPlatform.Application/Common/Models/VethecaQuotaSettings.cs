namespace VetPlatform.Application.Common.Models;

public class VethecaQuotaSettings
{
    public const string SectionName = "Vetheca:Quota";

    // Questions per calendar month (Costa Rica time) for accounts whose only Vetheca-capable
    // role is "Veterinario Vetheca". Accounts with a full clinic role
    // (Administrador/Veterinario) are unlimited for now. Every ask costs real
    // money (LLM call), so this caps exposure while pilot usage is measured.
    public int VethecaVeterinarianMonthlyLimit { get; set; } = 100;
}
