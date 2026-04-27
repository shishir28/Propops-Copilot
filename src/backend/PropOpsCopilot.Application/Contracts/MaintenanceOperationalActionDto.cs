using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceOperationalActionDto(
    Guid Id,
    Guid MaintenanceRequestId,
    MaintenanceOperationalActionType ActionType,
    string Detail,
    string ExternalReference,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc);
