using MediatR;
using VetPlatform.Application.Audit.Models;

namespace VetPlatform.Application.Audit.Queries.GetAuditLog;

public record GetAuditLogQuery(DateTime? FromUtc = null, DateTime? ToUtc = null) : IRequest<IReadOnlyList<AuditEntryDto>>;
