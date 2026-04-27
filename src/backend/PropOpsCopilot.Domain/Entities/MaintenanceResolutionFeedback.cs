using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Domain.Entities;

public sealed class MaintenanceResolutionFeedback
{
    private MaintenanceResolutionFeedback()
    {
    }

    public Guid Id { get; private set; }

    public Guid MaintenanceRequestId { get; private set; }

    public MaintenanceRequest? MaintenanceRequest { get; private set; }

    public Guid? MaintenanceTriageReviewId { get; private set; }

    public MaintenanceTriageReview? MaintenanceTriageReview { get; private set; }

    public string FinalResolution { get; private set; } = string.Empty;

    public MaintenanceRequestCategory CorrectedCategory { get; private set; }

    public MaintenanceRequestPriority CorrectedPriority { get; private set; }

    public string FinalTenantResponse { get; private set; } = string.Empty;

    public MaintenanceDispatchOutcome DispatchOutcome { get; private set; }

    public string ResolutionNotes { get; private set; } = string.Empty;

    public bool ExcludeFromTraining { get; private set; }

    public string ExclusionReason { get; private set; } = string.Empty;

    public string ResolvedBy { get; private set; } = string.Empty;

    public DateTimeOffset ResolvedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static MaintenanceResolutionFeedback Create(
        Guid maintenanceRequestId,
        Guid? maintenanceTriageReviewId,
        string finalResolution,
        MaintenanceRequestCategory correctedCategory,
        MaintenanceRequestPriority correctedPriority,
        string finalTenantResponse,
        MaintenanceDispatchOutcome dispatchOutcome,
        string resolutionNotes,
        bool excludeFromTraining,
        string exclusionReason,
        string resolvedBy)
    {
        if (maintenanceRequestId == Guid.Empty)
            throw new ArgumentException("Maintenance request id is required.", nameof(maintenanceRequestId));

        if (string.IsNullOrWhiteSpace(finalResolution))
            throw new ArgumentException("Final resolution is required.", nameof(finalResolution));

        if (string.IsNullOrWhiteSpace(finalTenantResponse))
            throw new ArgumentException("Final tenant response is required.", nameof(finalTenantResponse));

        if (excludeFromTraining && string.IsNullOrWhiteSpace(exclusionReason))
            throw new ArgumentException("Exclusion reason is required when feedback is excluded from training.", nameof(exclusionReason));

        var now = DateTimeOffset.UtcNow;
        return new MaintenanceResolutionFeedback
        {
            Id = Guid.NewGuid(),
            MaintenanceRequestId = maintenanceRequestId,
            MaintenanceTriageReviewId = maintenanceTriageReviewId,
            FinalResolution = finalResolution.Trim(),
            CorrectedCategory = correctedCategory,
            CorrectedPriority = correctedPriority,
            FinalTenantResponse = finalTenantResponse.Trim(),
            DispatchOutcome = dispatchOutcome,
            ResolutionNotes = resolutionNotes.Trim(),
            ExcludeFromTraining = excludeFromTraining,
            ExclusionReason = exclusionReason.Trim(),
            ResolvedBy = string.IsNullOrWhiteSpace(resolvedBy) ? "staff" : resolvedBy.Trim(),
            ResolvedAtUtc = now,
            CreatedAtUtc = now
        };
    }
}
