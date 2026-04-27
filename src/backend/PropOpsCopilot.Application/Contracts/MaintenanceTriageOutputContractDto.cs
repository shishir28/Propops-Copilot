namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageOutputContractDto(
    string Category,
    string Priority,
    string VendorType,
    string DispatchDecision,
    string InternalSummary,
    string TenantResponseDraft);
