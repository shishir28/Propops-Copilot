using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record CreateMaintenanceRequestCommand(
    string SubmitterName,
    string EmailAddress,
    string PhoneNumber,
    string PropertyName,
    string UnitNumber,
    string Description,
    MaintenanceRequestCategory Category,
    MaintenanceRequestPriority Priority,
    IntakeChannel Channel);
