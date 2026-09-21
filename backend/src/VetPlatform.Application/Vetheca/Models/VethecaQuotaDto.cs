namespace VetPlatform.Application.Vetheca.Models;

// MonthlyLimit/Remaining are null when the current user has no question limit.
public record VethecaQuotaDto(int? MonthlyLimit, int UsedThisMonth, int? Remaining, DateTime ResetsAtUtc);
