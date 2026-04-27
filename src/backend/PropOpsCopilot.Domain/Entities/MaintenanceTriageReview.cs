using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Domain.Entities;

public sealed class MaintenanceTriageReview
{
    private MaintenanceTriageReview()
    {
    }

    public Guid Id { get; private set; }

    public Guid MaintenanceRequestId { get; private set; }

    public MaintenanceRequest? MaintenanceRequest { get; private set; }

    public MaintenanceRequestCategory AiCategory { get; private set; }

    public MaintenanceRequestPriority AiPriority { get; private set; }

    public string AiVendorType { get; private set; } = string.Empty;

    public string AiDispatchDecision { get; private set; } = string.Empty;

    public string AiInternalSummary { get; private set; } = string.Empty;

    public string AiTenantResponseDraft { get; private set; } = string.Empty;

    public MaintenanceRequestCategory FinalCategory { get; private set; }

    public MaintenanceRequestPriority FinalPriority { get; private set; }

    public string FinalVendorType { get; private set; } = string.Empty;

    public string FinalDispatchDecision { get; private set; } = string.Empty;

    public string FinalInternalSummary { get; private set; } = string.Empty;

    public string FinalTenantResponseDraft { get; private set; } = string.Empty;

    public bool GuardrailRequiresHumanReview { get; private set; }

    public string GuardrailSummary { get; private set; } = string.Empty;

    public MaintenanceTriageReviewStatus Status { get; private set; }

    public string ReviewedBy { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ReviewedAtUtc { get; private set; }

    public static MaintenanceTriageReview Create(
        Guid maintenanceRequestId,
        MaintenanceRequestCategory aiCategory,
        MaintenanceRequestPriority aiPriority,
        string aiVendorType,
        string aiDispatchDecision,
        string aiInternalSummary,
        string aiTenantResponseDraft,
        MaintenanceRequestCategory finalCategory,
        MaintenanceRequestPriority finalPriority,
        string finalVendorType,
        string finalDispatchDecision,
        string finalInternalSummary,
        string finalTenantResponseDraft,
        bool guardrailRequiresHumanReview,
        string guardrailSummary,
        string reviewedBy)
    {
        if (maintenanceRequestId == Guid.Empty)
            throw new ArgumentException("Maintenance request id is required.", nameof(maintenanceRequestId));

        if (string.IsNullOrWhiteSpace(finalDispatchDecision))
            throw new ArgumentException("Dispatch decision is required.", nameof(finalDispatchDecision));

        if (string.IsNullOrWhiteSpace(finalTenantResponseDraft))
            throw new ArgumentException("Tenant response draft is required.", nameof(finalTenantResponseDraft));

        var now = DateTimeOffset.UtcNow;
        var status = aiCategory == finalCategory
                     && aiPriority == finalPriority
                     && string.Equals(aiVendorType.Trim(), finalVendorType.Trim(), StringComparison.Ordinal)
                     && string.Equals(aiDispatchDecision.Trim(), finalDispatchDecision.Trim(), StringComparison.Ordinal)
                     && string.Equals(aiInternalSummary.Trim(), finalInternalSummary.Trim(), StringComparison.Ordinal)
                     && string.Equals(aiTenantResponseDraft.Trim(), finalTenantResponseDraft.Trim(), StringComparison.Ordinal)
            ? MaintenanceTriageReviewStatus.Approved
            : MaintenanceTriageReviewStatus.Edited;

        return new MaintenanceTriageReview
        {
            Id = Guid.NewGuid(),
            MaintenanceRequestId = maintenanceRequestId,
            AiCategory = aiCategory,
            AiPriority = aiPriority,
            AiVendorType = aiVendorType.Trim(),
            AiDispatchDecision = aiDispatchDecision.Trim(),
            AiInternalSummary = aiInternalSummary.Trim(),
            AiTenantResponseDraft = aiTenantResponseDraft.Trim(),
            FinalCategory = finalCategory,
            FinalPriority = finalPriority,
            FinalVendorType = finalVendorType.Trim(),
            FinalDispatchDecision = finalDispatchDecision.Trim(),
            FinalInternalSummary = finalInternalSummary.Trim(),
            FinalTenantResponseDraft = finalTenantResponseDraft.Trim(),
            GuardrailRequiresHumanReview = guardrailRequiresHumanReview,
            GuardrailSummary = guardrailSummary.Trim(),
            Status = status,
            ReviewedBy = string.IsNullOrWhiteSpace(reviewedBy) ? "staff" : reviewedBy.Trim(),
            CreatedAtUtc = now,
            ReviewedAtUtc = now
        };
    }
}
