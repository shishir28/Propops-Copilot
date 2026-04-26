using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceRequestDto(
    Guid Id,
    string ReferenceNumber,
    string SubmitterName,
    string EmailAddress,
    string PhoneNumber,
    string PropertyName,
    string UnitNumber,
    string Description,
    string InternalSummary,
    string AssignedTeam,
    MaintenanceRequestCategory Category,
    MaintenanceRequestPriority Priority,
    MaintenanceRequestStatus Status,
    IntakeChannel Channel,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset TargetResponseByUtc);
