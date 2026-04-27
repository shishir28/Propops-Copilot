namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceOperationsDetailDto(
    MaintenanceRequestDto Request,
    MaintenanceTriageReviewDto? LatestReview,
    MaintenanceResolutionFeedbackDto? LatestFeedback,
    IReadOnlyList<MaintenanceOperationalActionDto> Actions);
