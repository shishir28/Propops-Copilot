namespace PropOpsCopilot.Application.Contracts;

public sealed record DashboardOverviewDto(
    int OpenRequests,
    int UrgentRequests,
    int TodaySubmissions,
    decimal AverageResponseHours,
    IReadOnlyList<MaintenanceRequestDto> RecentRequests);
