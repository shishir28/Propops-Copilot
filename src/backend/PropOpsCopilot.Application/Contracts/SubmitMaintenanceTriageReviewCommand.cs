using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record SubmitMaintenanceTriageReviewCommand(
    MaintenanceTriageOutputContractDto AiOutput,
    MaintenanceTriageGuardrailResultDto Guardrails,
    MaintenanceRequestCategory Category,
    MaintenanceRequestPriority Priority,
    string VendorType,
    string DispatchDecision,
    string InternalSummary,
    string TenantResponseDraft);
