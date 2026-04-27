namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageGuardrailIssueDto(
    string Code,
    string Severity,
    string Message);
