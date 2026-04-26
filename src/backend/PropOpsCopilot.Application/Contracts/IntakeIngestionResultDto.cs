namespace PropOpsCopilot.Application.Contracts;

public sealed record IntakeIngestionResultDto(
    IntakeSubmissionDto Submission,
    MaintenanceRequestDto MaintenanceRequest);
