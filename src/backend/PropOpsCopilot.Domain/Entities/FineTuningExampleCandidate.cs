using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Domain.Entities;

public sealed class FineTuningExampleCandidate
{
    private FineTuningExampleCandidate()
    {
    }

    public Guid Id { get; private set; }

    public Guid MaintenanceRequestId { get; private set; }

    public Guid MaintenanceResolutionFeedbackId { get; private set; }

    public MaintenanceResolutionFeedback? MaintenanceResolutionFeedback { get; private set; }

    public FineTuningCandidateStatus Status { get; private set; }

    public string InputSnapshotJson { get; private set; } = string.Empty;

    public string OutputSnapshotJson { get; private set; } = string.Empty;

    public string MetadataSnapshotJson { get; private set; } = string.Empty;

    public string ExclusionReason { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static FineTuningExampleCandidate Create(
        Guid maintenanceRequestId,
        Guid maintenanceResolutionFeedbackId,
        FineTuningCandidateStatus status,
        string inputSnapshotJson,
        string outputSnapshotJson,
        string metadataSnapshotJson,
        string exclusionReason)
    {
        if (maintenanceRequestId == Guid.Empty)
            throw new ArgumentException("Maintenance request id is required.", nameof(maintenanceRequestId));

        if (maintenanceResolutionFeedbackId == Guid.Empty)
            throw new ArgumentException("Resolution feedback id is required.", nameof(maintenanceResolutionFeedbackId));

        if (string.IsNullOrWhiteSpace(inputSnapshotJson))
            throw new ArgumentException("Input snapshot is required.", nameof(inputSnapshotJson));

        if (string.IsNullOrWhiteSpace(outputSnapshotJson))
            throw new ArgumentException("Output snapshot is required.", nameof(outputSnapshotJson));

        return new FineTuningExampleCandidate
        {
            Id = Guid.NewGuid(),
            MaintenanceRequestId = maintenanceRequestId,
            MaintenanceResolutionFeedbackId = maintenanceResolutionFeedbackId,
            Status = status,
            InputSnapshotJson = inputSnapshotJson,
            OutputSnapshotJson = outputSnapshotJson,
            MetadataSnapshotJson = metadataSnapshotJson.Trim(),
            ExclusionReason = exclusionReason.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
