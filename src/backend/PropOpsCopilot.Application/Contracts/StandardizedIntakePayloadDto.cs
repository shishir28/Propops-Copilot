using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record StandardizedIntakePayloadDto(
    IntakeChannel Channel,
    string SourceReference,
    DateTimeOffset ReceivedAtUtc,
    string SubmitterName,
    string TenantName,
    string EmailAddress,
    string PhoneNumber,
    string PropertyName,
    string UnitNumber,
    string Subject,
    string RawContent,
    string NormalizedContent,
    MaintenanceRequestCategory Category,
    MaintenanceRequestPriority Priority,
    bool IsAfterHours,
    bool MetadataMatched);
