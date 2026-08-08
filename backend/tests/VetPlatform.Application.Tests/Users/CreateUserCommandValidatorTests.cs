using VetPlatform.Application.Users.Commands.CreateUser;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Tests.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Allows_Known_Roles()
    {
        foreach (var role in RoleNames.All)
        {
            var result = _validator.Validate(new CreateUserCommand(
                $"user-{Guid.NewGuid():N}@vetplatform.test",
                "Password123!",
                "Test User",
                role));

            Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    [Fact]
    public void Rejects_Unknown_Roles()
    {
        var result = _validator.Validate(new CreateUserCommand(
            "user@vetplatform.test",
            "Password123!",
            "Test User",
            "Owner"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Role));
    }
}
