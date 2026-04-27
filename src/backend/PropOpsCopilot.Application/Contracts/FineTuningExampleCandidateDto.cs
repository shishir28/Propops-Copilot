using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Contracts;

public sealed record FineTuningExampleCandidateDto(
    Guid Id,
    Guid MaintenanceRequestId,
    Guid MaintenanceResolutionFeedbackId,
    FineTuningCandidateStatus Status,
    string InputSnapshotJson,
    string OutputSnapshotJson,
    string MetadataSnapshotJson,
    string ExclusionReason,
    DateTimeOffset CreatedAtUtc);
