namespace PropOpsCopilot.Application.Contracts;

public sealed record IngestSmsChatIntakeCommand(
    string SubmitterName,
    string PhoneNumber,
    string MessageBody,
    string EmailAddress,
    string PropertyHint,
    string UnitHint,
    string SourceReference,
    DateTimeOffset? ReceivedAtUtc);
