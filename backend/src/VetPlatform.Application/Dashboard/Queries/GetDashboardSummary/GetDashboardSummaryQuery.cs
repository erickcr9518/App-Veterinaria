using MediatR;
using VetPlatform.Application.Dashboard.Models;

namespace VetPlatform.Application.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
