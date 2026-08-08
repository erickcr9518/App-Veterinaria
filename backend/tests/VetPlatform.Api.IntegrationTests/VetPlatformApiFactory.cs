using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Identity;
using VetPlatform.Infrastructure.Persistence;

namespace VetPlatform.Api.IntegrationTests;

public sealed class VetPlatformApiFactory : WebApplicationFactory<Program>
{
    public const string DemoAdminEmail = "admin@vetplatform.dev";
    public const string DemoAdminPassword = "Admin123!";

    private readonly string _databaseName = $"VetPlatformTests-{Guid.NewGuid()}";

    public VetPlatformApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", "VetPlatform.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "VetPlatform.Tests.Client");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-32-bytes-minimum");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", "30");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpirationDays", "7");
        Environment.SetEnvironmentVariable("Seed__DemoData", "true");
        Environment.SetEnvironmentVariable("Seed__DemoAdminPassword", DemoAdminPassword);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=VetPlatformTests;Trusted_Connection=True",
                ["Jwt:Issuer"] = "VetPlatform.Tests",
                ["Jwt:Audience"] = "VetPlatform.Tests.Client",
                ["Jwt:SigningKey"] = "integration-test-signing-key-32-bytes-minimum",
                ["Jwt:AccessTokenExpirationMinutes"] = "30",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Seed:DemoData"] = "true",
                ["Seed:DemoAdminPassword"] = DemoAdminPassword,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        });
    }

    public async Task<(Guid ClinicId, Guid UserId)> CreateClinicUserAsync(
        string email,
        string role,
        string password = "Password123!")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var clinic = new Clinic
        {
            Name = $"Clinic {Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@clinic.test",
        };
        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = email,
            ClinicId = clinic.Id,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

        return (clinic.Id, user.Id);
    }

    public async Task<Guid> CreatePlatformAdministratorAsync(
        string email,
        string password = "Password123!")
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Platform Admin",
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, RoleNames.PlatformAdministrator);
        Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

        return user.Id;
    }
}
