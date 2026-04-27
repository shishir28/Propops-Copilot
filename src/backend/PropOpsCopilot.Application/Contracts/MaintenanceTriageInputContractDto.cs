namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageInputContractDto(
    string RequestId,
    string ReferenceNumber,
    string NormalizedText,
    string PropertyName,
    string? UnitNumber,
    string Channel,
    string? CategoryHint,
    string? PriorityHint,
    bool IsAfterHours,
    DateTimeOffset SubmittedAtUtc);
