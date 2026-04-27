namespace PropOpsCopilot.Domain.Enums;

public enum MaintenanceDispatchOutcome
{
    Completed,
    Escalated,
    Duplicate,
    Cancelled,
    NoAccess,
    VendorUnavailable,
    TenantResolved,
    NotMaintenance
}
