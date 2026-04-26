namespace PropOpsCopilot.Application.Contracts;

public sealed record IngestPhoneNoteIntakeCommand(
    string SubmitterName,
    string PhoneNumber,
    string EmailAddress,
    string Note,
    string PropertyHint,
    string UnitHint,
    string SourceReference,
    DateTimeOffset? ReceivedAtUtc);
