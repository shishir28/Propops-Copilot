namespace PropOpsCopilot.Application.Contracts;

public sealed record IntakeSubmissionDto(
    Guid Id,
    StandardizedIntakePayloadDto StandardizedPayload,
    Guid MaintenanceRequestId,
    string MaintenanceRequestReferenceNumber);
