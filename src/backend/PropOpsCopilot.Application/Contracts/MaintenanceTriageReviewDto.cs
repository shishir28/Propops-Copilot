using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageReviewDto(
    Guid Id,
    Guid MaintenanceRequestId,
    MaintenanceRequestCategory AiCategory,
    MaintenanceRequestPriority AiPriority,
    string AiVendorType,
    string AiDispatchDecision,
    string AiInternalSummary,
    string AiTenantResponseDraft,
    MaintenanceRequestCategory FinalCategory,
    MaintenanceRequestPriority FinalPriority,
    string FinalVendorType,
    string FinalDispatchDecision,
    string FinalInternalSummary,
    string FinalTenantResponseDraft,
    bool GuardrailRequiresHumanReview,
    string GuardrailSummary,
    MaintenanceTriageReviewStatus Status,
    string ReviewedBy,
    DateTimeOffset ReviewedAtUtc);
