namespace VetPlatform.Application.Common.Exceptions;

public class QuotaExceededException : Exception
{
    public const string VethecaMonthlyQuotaCode = "vetheca_quota_exceeded";

    public string Code { get; }
    public DateTime ResetsAtUtc { get; }

    public QuotaExceededException(string code, string message, DateTime resetsAtUtc)
        : base(message)
    {
        Code = code;
        ResetsAtUtc = resetsAtUtc;
    }
}
