using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Common;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Identity;
using VetPlatform.Infrastructure.Persistence.Interceptors;

namespace VetPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly AuditableEntitySaveChangesInterceptor _auditInterceptor;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntitySaveChangesInterceptor auditInterceptor,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _currentUserService = currentUserService;
    }

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientWeight> PatientWeights => Set<PatientWeight>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            var filter = BuildGlobalFilter(clrType);
            if (filter is not null)
            {
                builder.Entity(clrType).HasQueryFilter(filter);
            }
        }
    }

    private Guid? CurrentClinicId => _currentUserService.ClinicId;

    private bool IsPlatformAdministrator => _currentUserService.Role == RoleNames.PlatformAdministrator;

    private LambdaExpression? BuildGlobalFilter(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        Expression? filter = null;

        if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
        {
            filter = CombineFilter(filter, BuildSoftDeleteFilter(parameter));
        }

        if (typeof(ITenantEntity).IsAssignableFrom(clrType))
        {
            filter = CombineFilter(filter, BuildTenantFilter(parameter));
        }

        return filter is null ? null : Expression.Lambda(filter, parameter);
    }

    private static Expression BuildSoftDeleteFilter(ParameterExpression parameter)
    {
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        return Expression.Equal(property, Expression.Constant(false));
    }

    private Expression BuildTenantFilter(ParameterExpression parameter)
    {
        var entityClinicId = Expression.Convert(
            Expression.Property(parameter, nameof(ITenantEntity.ClinicId)),
            typeof(Guid?));
        var currentClinicId = Expression.Property(Expression.Constant(this), nameof(CurrentClinicId));
        var isPlatformAdministrator = Expression.Property(Expression.Constant(this), nameof(IsPlatformAdministrator));

        var belongsToCurrentClinic = Expression.AndAlso(
            Expression.NotEqual(currentClinicId, Expression.Constant(null, typeof(Guid?))),
            Expression.Equal(entityClinicId, currentClinicId));

        return Expression.OrElse(isPlatformAdministrator, belongsToCurrentClinic);
    }

    private static Expression CombineFilter(Expression? current, Expression next)
    {
        return current is null ? next : Expression.AndAlso(current, next);
    }
}
