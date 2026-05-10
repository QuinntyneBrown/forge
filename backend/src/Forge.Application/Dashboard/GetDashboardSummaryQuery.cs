using MediatR;

namespace Forge.Application.Dashboard;

public record GetDashboardSummaryQuery() : IRequest<DashboardSummaryDto>;
