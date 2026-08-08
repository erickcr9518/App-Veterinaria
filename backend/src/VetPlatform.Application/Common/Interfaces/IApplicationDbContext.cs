using Microsoft.EntityFrameworkCore;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Clinic> Clinics { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
