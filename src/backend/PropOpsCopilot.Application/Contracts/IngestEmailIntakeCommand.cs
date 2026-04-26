namespace PropOpsCopilot.Application.Contracts;

public sealed record IngestEmailIntakeCommand(
    string SubmitterName,
    string EmailAddress,
    string Subject,
    string MessageBody,
    string PhoneNumber,
    string PropertyHint,
    string UnitHint,
    string SourceReference,
    DateTimeOffset? ReceivedAtUtc);
