namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceOperationsDetailDto(
    MaintenanceRequestDto Request,
    MaintenanceTriageReviewDto? LatestReview,
    IReadOnlyList<MaintenanceOperationalActionDto> Actions);
