using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Options;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Services;

public sealed partial class OmnichannelIntakeService(
    IIntakeSubmissionRepository intakeSubmissionRepository,
    IContactDirectoryRepository contactDirectoryRepository,
    IOptions<IntakePreprocessingOptions> preprocessingOptions)
{
    private readonly IntakePreprocessingOptions intakeOptions = preprocessingOptions.Value;

    public Task<IReadOnlyList<IntakeSubmissionDto>> ListRecentAsync(CancellationToken cancellationToken = default) =>
        ListRecentInternalAsync(cancellationToken);

    public Task<IntakeIngestionResultDto> IngestEmailAsync(
        IngestEmailIntakeCommand command,
        CancellationToken cancellationToken = default) =>
        IngestAsync(
            channel: IntakeChannel.Email,
            submitterName: command.SubmitterName,
            emailAddress: command.EmailAddress,
            phoneNumber: command.PhoneNumber,
            subject: command.Subject,
            rawContent: command.MessageBody,
            propertyHint: command.PropertyHint,
            unitHint: command.UnitHint,
            sourceReference: command.SourceReference,
            receivedAtUtc: command.ReceivedAtUtc,
            cancellationToken: cancellationToken);

    public Task<IntakeIngestionResultDto> IngestSmsChatAsync(
        IngestSmsChatIntakeCommand command,
        CancellationToken cancellationToken = default) =>
        IngestAsync(
            channel: IntakeChannel.SmsChat,
            submitterName: command.SubmitterName,
            emailAddress: command.EmailAddress,
            phoneNumber: command.PhoneNumber,
            subject: string.Empty,
            rawContent: command.MessageBody,
            propertyHint: command.PropertyHint,
            unitHint: command.UnitHint,
            sourceReference: command.SourceReference,
            receivedAtUtc: command.ReceivedAtUtc,
            cancellationToken: cancellationToken);

    public Task<IntakeIngestionResultDto> IngestPhoneNoteAsync(
        IngestPhoneNoteIntakeCommand command,
        CancellationToken cancellationToken = default) =>
        IngestAsync(
            channel: IntakeChannel.PhoneNote,
            submitterName: command.SubmitterName,
            emailAddress: command.EmailAddress,
            phoneNumber: command.PhoneNumber,
            subject: "Phone note",
            rawContent: command.Note,
            propertyHint: command.PropertyHint,
            unitHint: command.UnitHint,
            sourceReference: command.SourceReference,
            receivedAtUtc: command.ReceivedAtUtc,
            cancellationToken: cancellationToken);

    private async Task<IReadOnlyList<IntakeSubmissionDto>> ListRecentInternalAsync(CancellationToken cancellationToken)
    {
        var submissions = await intakeSubmissionRepository.ListRecentAsync(take: 12, cancellationToken: cancellationToken);
        return submissions
            .Where(submission => submission.MaintenanceRequest is not null)
            .Select(Map)
            .ToArray();
    }

    private async Task<IntakeIngestionResultDto> IngestAsync(
        IntakeChannel channel,
        string submitterName,
        string emailAddress,
        string phoneNumber,
        string subject,
        string rawContent,
        string propertyHint,
        string unitHint,
        string sourceReference,
        DateTimeOffset? receivedAtUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new ArgumentException("Message content is required.", nameof(rawContent));
        }

        if (channel == IntakeChannel.Email && string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new ArgumentException("Email ingestion requires an email address.", nameof(emailAddress));
        }

        if (channel == IntakeChannel.SmsChat && string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("SMS or chat ingestion requires a phone number.", nameof(phoneNumber));
        }

        var receivedAt = receivedAtUtc ?? DateTimeOffset.UtcNow;
        var normalizedEmail = NormalizeEmail(emailAddress);
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        var directoryEntry = await ResolveDirectoryEntryAsync(normalizedEmail, normalizedPhone, cancellationToken);
        var metadata = ResolveMetadata(directoryEntry, submitterName, normalizedEmail, normalizedPhone, propertyHint, unitHint);
        var normalizedContent = NormalizeContent(subject, rawContent);
        var isAfterHours = DetectAfterHours(receivedAt);
        var category = InferCategory(normalizedContent);
        var priority = InferPriority(normalizedContent);
        var standardizedPayload = new StandardizedIntakePayloadDto(
            channel,
            ResolveSourceReference(sourceReference, channel),
            receivedAt,
            metadata.SubmitterName,
            metadata.TenantName,
            metadata.EmailAddress,
            metadata.PhoneNumber,
            metadata.PropertyName,
            metadata.UnitNumber,
            NormalizeSingleLine(subject),
            rawContent.Trim(),
            normalizedContent,
            category,
            priority,
            isAfterHours,
            metadata.MetadataMatched);

        var maintenanceRequest = MaintenanceRequest.Create(
            standardizedPayload.SubmitterName,
            standardizedPayload.EmailAddress,
            standardizedPayload.PhoneNumber,
            standardizedPayload.PropertyName,
            standardizedPayload.UnitNumber,
            standardizedPayload.NormalizedContent,
            standardizedPayload.Category,
            standardizedPayload.Priority,
            standardizedPayload.Channel);

        var submission = IntakeSubmission.Create(
            standardizedPayload.SourceReference,
            standardizedPayload.Channel,
            standardizedPayload.ReceivedAtUtc,
            standardizedPayload.SubmitterName,
            standardizedPayload.TenantName,
            standardizedPayload.EmailAddress,
            standardizedPayload.PhoneNumber,
            standardizedPayload.PropertyName,
            standardizedPayload.UnitNumber,
            standardizedPayload.Subject,
            standardizedPayload.RawContent,
            standardizedPayload.NormalizedContent,
            standardizedPayload.Category,
            standardizedPayload.Priority,
            standardizedPayload.IsAfterHours,
            standardizedPayload.MetadataMatched,
            maintenanceRequest);

        await intakeSubmissionRepository.AddWithRequestAsync(submission, maintenanceRequest, cancellationToken);

        return new IntakeIngestionResultDto(
            Map(submission),
            MaintenanceRequestService.Map(maintenanceRequest));
    }

    private static IntakeSubmissionDto Map(IntakeSubmission submission) =>
        new(
            submission.Id,
            new StandardizedIntakePayloadDto(
                submission.Channel,
                submission.SourceReference,
                submission.ReceivedAtUtc,
                submission.SubmitterName,
                submission.TenantName,
                submission.EmailAddress,
                submission.PhoneNumber,
                submission.PropertyName,
                submission.UnitNumber,
                submission.Subject,
                submission.RawContent,
                submission.NormalizedContent,
                submission.Category,
                submission.Priority,
                submission.IsAfterHours,
                submission.MetadataMatched),
            submission.MaintenanceRequestId,
            submission.MaintenanceRequest?.ReferenceNumber ?? string.Empty);

    private async Task<ContactDirectoryEntry?> ResolveDirectoryEntryAsync(
        string emailAddress,
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(emailAddress))
        {
            var emailMatch = await contactDirectoryRepository.FindByEmailAsync(emailAddress, cancellationToken);
            if (emailMatch is not null)
            {
                return emailMatch;
            }
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            return await contactDirectoryRepository.FindByPhoneAsync(phoneNumber, cancellationToken);
        }

        return null;
    }

    private MetadataResolution ResolveMetadata(
        ContactDirectoryEntry? directoryEntry,
        string submitterName,
        string emailAddress,
        string phoneNumber,
        string propertyHint,
        string unitHint)
    {
        var resolvedSubmitterName = FirstNonEmpty(submitterName, directoryEntry?.FullName);
        var resolvedPropertyName = FirstNonEmpty(propertyHint, directoryEntry?.PropertyName);

        if (string.IsNullOrWhiteSpace(resolvedSubmitterName))
        {
            throw new InvalidOperationException("Unable to resolve the submitter name from the intake payload.");
        }

        if (string.IsNullOrWhiteSpace(resolvedPropertyName))
        {
            throw new InvalidOperationException(
                "Unable to resolve property metadata. Provide a property hint or send the intake from a known contact.");
        }

        return new MetadataResolution(
            resolvedSubmitterName,
            FirstNonEmpty(directoryEntry?.TenantName, resolvedSubmitterName),
            FirstNonEmpty(emailAddress, directoryEntry?.EmailAddress),
            FirstNonEmpty(phoneNumber, directoryEntry?.PhoneNumber),
            resolvedPropertyName,
            FirstNonEmpty(unitHint, directoryEntry?.UnitNumber),
            directoryEntry is not null);
    }

    private bool DetectAfterHours(DateTimeOffset receivedAtUtc)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(intakeOptions.TimeZoneId);
        var localTime = TimeZoneInfo.ConvertTime(receivedAtUtc, timeZone);

        return localTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
               || localTime.Hour < intakeOptions.BusinessHoursStartHour
               || localTime.Hour >= intakeOptions.BusinessHoursEndHour;
    }

    private static string ResolveSourceReference(string sourceReference, IntakeChannel channel)
    {
        var normalized = NormalizeSingleLine(sourceReference);
        return string.IsNullOrWhiteSpace(normalized)
            ? $"{channel.ToString().ToUpperInvariant()}-{Guid.NewGuid():N}"[..24]
            : normalized;
    }

    private static string NormalizeContent(string subject, string rawContent)
    {
        var normalizedSubject = NormalizeSingleLine(subject);
        var normalizedBody = string.Join(
            Environment.NewLine,
            rawContent
                .ReplaceLineEndings("\n")
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(line => MultiWhitespaceRegex().Replace(line, " "))
                .Where(line => !string.IsNullOrWhiteSpace(line)));

        return string.IsNullOrWhiteSpace(normalizedSubject)
            ? normalizedBody
            : string.IsNullOrWhiteSpace(normalizedBody)
                ? normalizedSubject
                : $"{normalizedSubject}{Environment.NewLine}{normalizedBody}";
    }

    private static string NormalizeSingleLine(string value) =>
        MultiWhitespaceRegex().Replace(value.Trim(), " ");

    private static string NormalizeEmail(string emailAddress) =>
        emailAddress.Trim().ToLowerInvariant();

    private static string NormalizePhoneNumber(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static MaintenanceRequestCategory InferCategory(string normalizedContent)
    {
        var content = normalizedContent.ToLowerInvariant();

        return content switch
        {
            _ when ContainsAny(content, "sink", "pipe", "leak", "toilet", "drain", "tap") =>
                MaintenanceRequestCategory.Plumbing,
            _ when ContainsAny(content, "power", "light", "switch", "electrical", "outlet", "breaker") =>
                MaintenanceRequestCategory.Electrical,
            _ when ContainsAny(content, "air conditioning", "ac ", "cooling", "heating", "hvac") =>
                MaintenanceRequestCategory.HVAC,
            _ when ContainsAny(content, "dishwasher", "oven", "fridge", "appliance", "washer", "dryer") =>
                MaintenanceRequestCategory.Appliances,
            _ when ContainsAny(content, "lock", "door", "security", "alarm", "window", "cannot secure") =>
                MaintenanceRequestCategory.Security,
            _ => MaintenanceRequestCategory.General
        };
    }

    private static MaintenanceRequestPriority InferPriority(string normalizedContent)
    {
        var content = normalizedContent.ToLowerInvariant();

        return content switch
        {
            _ when ContainsAny(
                content,
                "fire",
                "sparking",
                "smell gas",
                "flood",
                "burst pipe",
                "cannot secure",
                "no power",
                "emergency") => MaintenanceRequestPriority.Emergency,
            _ when ContainsAny(
                content,
                "urgent",
                "heavily",
                "leaking",
                "stuck",
                "not cooling",
                "overflow",
                "unsafe") => MaintenanceRequestPriority.High,
            _ when ContainsAny(content, "minor", "cosmetic", "whenever", "low priority") =>
                MaintenanceRequestPriority.Low,
            _ => MaintenanceRequestPriority.Normal
        };
    }

    private static bool ContainsAny(string content, params string[] terms) =>
        terms.Any(content.Contains);

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiWhitespaceRegex();

    private sealed record MetadataResolution(
        string SubmitterName,
        string TenantName,
        string EmailAddress,
        string PhoneNumber,
        string PropertyName,
        string UnitNumber,
        bool MetadataMatched);
}
