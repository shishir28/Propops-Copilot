using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Domain.Entities;

public sealed class IntakeSubmission
{
    private IntakeSubmission()
    {
    }

    public Guid Id { get; private set; }

    public string SourceReference { get; private set; } = string.Empty;

    public IntakeChannel Channel { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public string SubmitterName { get; private set; } = string.Empty;

    public string TenantName { get; private set; } = string.Empty;

    public string EmailAddress { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public string PropertyName { get; private set; } = string.Empty;

    public string UnitNumber { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string RawContent { get; private set; } = string.Empty;

    public string NormalizedContent { get; private set; } = string.Empty;

    public MaintenanceRequestCategory Category { get; private set; }

    public MaintenanceRequestPriority Priority { get; private set; }

    public bool IsAfterHours { get; private set; }

    public bool MetadataMatched { get; private set; }

    public Guid MaintenanceRequestId { get; private set; }

    public MaintenanceRequest? MaintenanceRequest { get; private set; }

    public static IntakeSubmission Create(
        string sourceReference,
        IntakeChannel channel,
        DateTimeOffset receivedAtUtc,
        string submitterName,
        string tenantName,
        string emailAddress,
        string phoneNumber,
        string propertyName,
        string unitNumber,
        string subject,
        string rawContent,
        string normalizedContent,
        MaintenanceRequestCategory category,
        MaintenanceRequestPriority priority,
        bool isAfterHours,
        bool metadataMatched,
        MaintenanceRequest maintenanceRequest)
    {
        if (channel == IntakeChannel.Portal)
        {
            throw new ArgumentException("Portal submissions do not use omnichannel intake.", nameof(channel));
        }

        if (string.IsNullOrWhiteSpace(submitterName))
        {
            throw new ArgumentException("Submitter name is required.", nameof(submitterName));
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name is required.", nameof(propertyName));
        }

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            throw new ArgumentException("Raw content is required.", nameof(rawContent));
        }

        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            throw new ArgumentException("Normalized content is required.", nameof(normalizedContent));
        }

        return new IntakeSubmission
        {
            Id = Guid.NewGuid(),
            SourceReference = sourceReference.Trim(),
            Channel = channel,
            ReceivedAtUtc = receivedAtUtc,
            SubmitterName = submitterName.Trim(),
            TenantName = string.IsNullOrWhiteSpace(tenantName) ? submitterName.Trim() : tenantName.Trim(),
            EmailAddress = emailAddress.Trim().ToLowerInvariant(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            PropertyName = propertyName.Trim(),
            UnitNumber = unitNumber.Trim(),
            Subject = subject.Trim(),
            RawContent = rawContent.Trim(),
            NormalizedContent = normalizedContent.Trim(),
            Category = category,
            Priority = priority,
            IsAfterHours = isAfterHours,
            MetadataMatched = metadataMatched,
            MaintenanceRequestId = maintenanceRequest.Id,
            MaintenanceRequest = maintenanceRequest
        };
    }

    private static string NormalizePhoneNumber(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());
}
