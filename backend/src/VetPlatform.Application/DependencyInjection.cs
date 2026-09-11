using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Application.Common.Behaviors;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Services;

namespace VetPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Pure business logic over IApplicationDbContext, no Infrastructure-only
        // dependency needed, so it's registered here rather than in Infrastructure.
        services.AddScoped<ILibraryChunkSearchService, LibraryChunkSearchService>();

        return services;
    }
}
