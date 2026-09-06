using VetPlatform.Application.Users.Commands.SetUserRoles;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Tests.Users;

public class SetUserRolesCommandValidatorTests
{
    private readonly SetUserRolesCommandValidator _validator = new();

    [Fact]
    public void Allows_Multiple_Clinic_Roles()
    {
        var result = _validator.Validate(new SetUserRolesCommand(
            Guid.NewGuid(),
            Roles: new[] { RoleNames.Administrator, RoleNames.Veterinarian }));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_Empty_Roles()
    {
        var result = _validator.Validate(new SetUserRolesCommand(Guid.NewGuid(), Roles: Array.Empty<string>()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SetUserRolesCommand.Roles));
    }

    [Fact]
    public void Rejects_Platform_Administrator_Mixed_With_Clinic_Roles()
    {
        var result = _validator.Validate(new SetUserRolesCommand(
            Guid.NewGuid(),
            Roles: new[] { RoleNames.PlatformAdministrator, RoleNames.Veterinarian }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SetUserRolesCommand.Roles));
    }
}
