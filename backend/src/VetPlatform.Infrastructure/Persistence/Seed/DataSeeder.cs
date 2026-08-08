using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Identity;

namespace VetPlatform.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public const string DemoAdminEmail = "admin@vetplatform.dev";
    public const string DemoPlatformAdminEmail = "superadmin@vetplatform.dev";

    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        bool seedDemoData,
        string? demoAdminPassword)
    {
        await SeedRolesAsync(roleManager, logger);
        await SeedPermissionsAsync(dbContext, logger);
        await SeedRolePermissionsAsync(dbContext, roleManager, logger);

        if (!seedDemoData)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(demoAdminPassword))
        {
            throw new InvalidOperationException("Seed:DemoAdminPassword es requerido cuando Seed:DemoData esta habilitado.");
        }

        await SeedDemoClinicAndAdminAsync(dbContext, userManager, logger, demoAdminPassword);
        await SeedDemoPlatformAdministratorAsync(userManager, logger, demoAdminPassword);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.NewGuid() });
            if (!result.Succeeded)
            {
                logger.LogError("No se pudo crear el rol {Role}: {Errors}", roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var existingCodes = await dbContext.Permissions.Select(p => p.Code).ToListAsync();

        foreach (var (code, module, description) in PermissionCodes.Catalog)
        {
            if (existingCodes.Contains(code))
            {
                continue;
            }

            dbContext.Permissions.Add(new Permission
            {
                Code = code,
                Module = module,
                Description = description,
            });
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task SeedRolePermissionsAsync(
        ApplicationDbContext dbContext, RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        var permissionsByCode = await dbContext.Permissions.ToDictionaryAsync(p => p.Code);

        foreach (var (roleName, permissionCodes) in RoleDefaultPermissions.Map)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                logger.LogWarning("Rol {Role} no encontrado al sembrar permisos.", roleName);
                continue;
            }

            var desiredPermissionIds = permissionCodes
                .Where(permissionsByCode.ContainsKey)
                .Select(code => permissionsByCode[code].Id)
                .ToHashSet();

            var existingRolePermissions = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync();

            var staleRolePermissions = existingRolePermissions
                .Where(rp => !desiredPermissionIds.Contains(rp.PermissionId))
                .ToList();

            if (staleRolePermissions.Count > 0)
            {
                dbContext.RolePermissions.RemoveRange(staleRolePermissions);
                logger.LogInformation(
                    "Se removieron {Count} permisos obsoletos del rol {Role}.",
                    staleRolePermissions.Count, roleName);
            }

            var existingPermissionIds = existingRolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            foreach (var permissionId in desiredPermissionIds)
            {
                if (existingPermissionIds.Contains(permissionId))
                {
                    continue;
                }

                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId,
                });
            }
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task SeedDemoClinicAndAdminAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string demoAdminPassword)
    {
        var clinic = await dbContext.Clinics.FirstOrDefaultAsync();
        if (clinic is null)
        {
            clinic = new Clinic
            {
                Name = "Clínica Veterinaria Demo",
                LegalId = "0000000000",
                Address = "Ficticio 123, Costa Rica",
                Phone = "+506 0000-0000",
                Email = "contacto@clinicademo.dev",
                TimeZone = "America/Costa_Rica",
            };
            dbContext.Clinics.Add(clinic);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            logger.LogInformation("Clínica de demostración creada: {ClinicId}", clinic.Id);
        }

        var existingAdmin = await userManager.FindByEmailAsync(DemoAdminEmail);
        if (existingAdmin is not null)
        {
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = DemoAdminEmail,
            Email = DemoAdminEmail,
            EmailConfirmed = true,
            FullName = "Administrador Demo",
            ClinicId = clinic.Id,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(adminUser, demoAdminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogError("No se pudo crear el usuario administrador de demostración: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(adminUser, RoleNames.Administrator);
        logger.LogInformation("Usuario administrador de demostración creado: {Email}", DemoAdminEmail);
    }

    private static async Task SeedDemoPlatformAdministratorAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string demoAdminPassword)
    {
        var existingPlatformAdmin = await userManager.FindByEmailAsync(DemoPlatformAdminEmail);
        if (existingPlatformAdmin is not null)
        {
            return;
        }

        var platformAdmin = new ApplicationUser
        {
            UserName = DemoPlatformAdminEmail,
            Email = DemoPlatformAdminEmail,
            EmailConfirmed = true,
            FullName = "Superadministrador Demo",
            ClinicId = null,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(platformAdmin, demoAdminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogError("No se pudo crear el usuario superadministrador de demostración: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(platformAdmin, RoleNames.PlatformAdministrator);
        logger.LogInformation("Usuario superadministrador de demostración creado: {Email}", DemoPlatformAdminEmail);
    }
}
