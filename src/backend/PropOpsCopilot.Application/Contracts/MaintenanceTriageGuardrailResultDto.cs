namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageGuardrailResultDto(
    bool SchemaValid,
    bool PolicyPassed,
    bool EmergencyKeywordCheckPassed,
    double ConfidenceScore,
    double ConfidenceThreshold,
    bool RequiresHumanReview,
    bool FallbackApplied,
    IReadOnlyList<MaintenanceTriageGuardrailIssueDto> Issues);
