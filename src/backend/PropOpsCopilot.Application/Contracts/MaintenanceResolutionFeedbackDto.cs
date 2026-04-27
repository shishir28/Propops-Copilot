using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceResolutionFeedbackDto(
    Guid Id,
    Guid MaintenanceRequestId,
    Guid? MaintenanceTriageReviewId,
    string FinalResolution,
    MaintenanceRequestCategory CorrectedCategory,
    MaintenanceRequestPriority CorrectedPriority,
    string FinalTenantResponse,
    MaintenanceDispatchOutcome DispatchOutcome,
    string ResolutionNotes,
    bool ExcludeFromTraining,
    string ExclusionReason,
    string ResolvedBy,
    DateTimeOffset ResolvedAtUtc);
