using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;
using VetPlatform.Domain.Constants;
using VetPlatform.Infrastructure.Identity;
using VetPlatform.Infrastructure.Persistence;
using VetPlatform.Infrastructure.Persistence.Interceptors;
using VetPlatform.Infrastructure.Vetheca;

namespace VetPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = ValidateJwtSettings(configuration);

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordResetEmailSender, PasswordResetEmailSender>();

        services.Configure<PubMedSettings>(configuration.GetSection(PubMedSettings.SectionName));
        services.AddHttpClient<IPubMedClient, PubMedClient>((provider, client) =>
        {
            var pubMedSettings = provider.GetRequiredService<IOptions<PubMedSettings>>().Value;
            client.BaseAddress = new Uri(pubMedSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.Configure<AnthropicSettings>(configuration.GetSection(AnthropicSettings.SectionName));
        services.AddHttpClient<ILlmClient, AnthropicLlmClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirstValue(CurrentUserService.UserIdClaimType)
                            ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var securityStamp = context.Principal?.FindFirstValue(CurrentUserService.SecurityStampClaimType);

                        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(securityStamp))
                        {
                            context.Fail("Token de acceso invalido.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());
                        if (user is null || !user.IsActive)
                        {
                            context.Fail("El usuario asociado al token ya no existe o esta inactivo.");
                            return;
                        }

                        var currentSecurityStamp = await userManager.GetSecurityStampAsync(user);
                        if (!string.Equals(currentSecurityStamp, securityStamp, StringComparison.Ordinal))
                        {
                            context.Fail("El token de acceso ya no es valido para este usuario.");
                        }
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var (code, _, _) in PermissionCodes.Catalog)
            {
                options.AddPolicy(code, policy =>
                    policy.RequireClaim(CurrentUserService.PermissionClaimType, code));
            }
        });

        return services;
    }

    private static JwtSettings ValidateJwtSettings(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Falta la seccion de configuracion 'Jwt'.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer es requerido.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience es requerido.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey es requerido. Configuralo con user-secrets, variables de entorno o appsettings.Development.json local.");
        }

        if (Encoding.UTF8.GetByteCount(jwtSettings.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 bytes para HS256.");
        }

        if (jwtSettings.AccessTokenExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:AccessTokenExpirationMinutes debe ser mayor que cero.");
        }

        if (jwtSettings.RefreshTokenExpirationDays <= 0)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenExpirationDays debe ser mayor que cero.");
        }

        return jwtSettings;
    }
}
