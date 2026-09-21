using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Common.Models;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Vetheca.Services;

// Counts a user's own VethecaSearchLog rows for the current calendar month
// (Costa Rica time) - every ask already writes one, so no separate counter to
// keep in sync. Only accounts whose sole Vetheca-capable role is "Veterinario
// Vetheca" are limited; full clinic roles are unlimited for now.
public class VethecaQuotaService : IVethecaQuotaService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly VethecaQuotaSettings _settings;

    public VethecaQuotaService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IOptions<VethecaQuotaSettings> settings)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _settings = settings.Value;
    }

    public async Task<VethecaQuotaDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        var limit = ResolveMonthlyLimit();
        var (monthStart, resetsAtUtc) = CurrentMonthWindow();
        var used = await CountUsedAsync(monthStart, cancellationToken);

        return new VethecaQuotaDto(limit, used, limit is null ? null : Math.Max(0, limit.Value - used), resetsAtUtc);
    }

    public async Task EnsureAvailableAsync(CancellationToken cancellationToken)
    {
        var limit = ResolveMonthlyLimit();
        if (limit is null)
        {
            return;
        }

        var (monthStart, resetsAtUtc) = CurrentMonthWindow();
        var used = await CountUsedAsync(monthStart, cancellationToken);
        if (used >= limit.Value)
        {
            throw new QuotaExceededException(
                QuotaExceededException.VethecaMonthlyQuotaCode,
                "Alcanzaste el límite de preguntas de este mes.",
                resetsAtUtc);
        }
    }

    private int? ResolveMonthlyLimit()
    {
        var roles = _currentUserService.Roles;
        var hasFullClinicRole = roles.Contains(RoleNames.Administrator) || roles.Contains(RoleNames.Veterinarian);

        return roles.Contains(RoleNames.VethecaVeterinarian) && !hasFullClinicRole
            ? Math.Max(0, _settings.VethecaVeterinarianMonthlyLimit)
            : null;
    }

    private Task<int> CountUsedAsync(DateTime monthStart, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        return _dbContext.VethecaSearchLogs
            .AsNoTracking()
            .CountAsync(v => v.CreatedByUserId == userId && v.CreatedAtUtc >= monthStart, cancellationToken);
    }

    // The month rolls over at midnight Costa Rica time (UTC-6, no daylight
    // saving) so "the 1st" matches what a Costa Rican user expects. Revisit if
    // the product ever needs per-user time zones.
    private static readonly TimeSpan MonthBoundaryOffset = TimeSpan.FromHours(-6);

    private static (DateTime MonthStart, DateTime ResetsAtUtc) CurrentMonthWindow()
    {
        var localNow = DateTime.UtcNow + MonthBoundaryOffset;
        var localMonthStart = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var monthStartUtc = DateTime.SpecifyKind(localMonthStart - MonthBoundaryOffset, DateTimeKind.Utc);
        return (monthStartUtc, monthStartUtc.AddMonths(1));
    }
}
