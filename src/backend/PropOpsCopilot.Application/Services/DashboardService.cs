using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Services;

public sealed class DashboardService(IMaintenanceRequestRepository maintenanceRequestRepository)
{
    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var requests = await maintenanceRequestRepository.ListAsync(cancellationToken);
        var today = DateTimeOffset.UtcNow.Date;

        return new DashboardOverviewDto(
            OpenRequests: requests.Count(request => request.Status != MaintenanceRequestStatus.Completed),
            UrgentRequests: requests.Count(request => request.Priority is MaintenanceRequestPriority.High or MaintenanceRequestPriority.Emergency),
            TodaySubmissions: requests.Count(request => request.SubmittedAtUtc.UtcDateTime.Date == today),
            AverageResponseHours: requests.Count == 0
                ? 0
                : Math.Round((decimal)requests.Average(request => (request.TargetResponseByUtc - request.SubmittedAtUtc).TotalHours), 1),
            RecentRequests: requests.Take(6).Select(MaintenanceRequestService.Map).ToArray());
    }
}
