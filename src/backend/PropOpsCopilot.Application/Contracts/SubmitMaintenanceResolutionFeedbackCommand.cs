using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record SubmitMaintenanceResolutionFeedbackCommand(
    string FinalResolution,
    MaintenanceRequestCategory CorrectedCategory,
    MaintenanceRequestPriority CorrectedPriority,
    string FinalTenantResponse,
    MaintenanceDispatchOutcome DispatchOutcome,
    string ResolutionNotes,
    bool ExcludeFromTraining,
    string ExclusionReason);
