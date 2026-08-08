namespace VetPlatform.Application.Common.Models;

public class UserAccountResult
{
    public bool Succeeded { get; init; }
    public Guid? UserId { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static UserAccountResult Success(Guid userId) => new() { Succeeded = true, UserId = userId };
    public static UserAccountResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}
