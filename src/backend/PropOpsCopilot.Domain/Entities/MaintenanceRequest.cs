using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Domain.Entities;

public sealed class MaintenanceRequest
{
    private MaintenanceRequest()
    {
    }

    public Guid Id { get; private set; }

    public string ReferenceNumber { get; private set; } = string.Empty;

    public string SubmitterName { get; private set; } = string.Empty;

    public string EmailAddress { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public string PropertyName { get; private set; } = string.Empty;

    public string UnitNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string InternalSummary { get; private set; } = string.Empty;

    public string AssignedTeam { get; private set; } = string.Empty;

    public MaintenanceRequestCategory Category { get; private set; }

    public MaintenanceRequestPriority Priority { get; private set; }

    public MaintenanceRequestStatus Status { get; private set; }

    public IntakeChannel Channel { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public DateTimeOffset TargetResponseByUtc { get; private set; }

    public static MaintenanceRequest Create(
        string submitterName,
        string emailAddress,
        string phoneNumber,
        string propertyName,
        string unitNumber,
        string description,
        MaintenanceRequestCategory category,
        MaintenanceRequestPriority priority,
        IntakeChannel channel)
    {
        if (string.IsNullOrWhiteSpace(submitterName))
        {
            throw new ArgumentException("Submitter name is required.", nameof(submitterName));
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name is required.", nameof(propertyName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        var submittedAt = DateTimeOffset.UtcNow;

        var referenceNumber = $"MR-{submittedAt:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

        return new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = referenceNumber[..25].ToUpperInvariant(),
            SubmitterName = submitterName.Trim(),
            EmailAddress = emailAddress.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            PropertyName = propertyName.Trim(),
            UnitNumber = unitNumber.Trim(),
            Description = description.Trim(),
            InternalSummary = BuildSummary(category, priority, description),
            AssignedTeam = ResolveAssignedTeam(category, priority),
            Category = category,
            Priority = priority,
            Status = MaintenanceRequestStatus.New,
            Channel = channel,
            SubmittedAtUtc = submittedAt,
            TargetResponseByUtc = submittedAt.AddHours(priority switch
            {
                MaintenanceRequestPriority.Emergency => 1,
                MaintenanceRequestPriority.High => 4,
                MaintenanceRequestPriority.Normal => 12,
                _ => 24
            })
        };
    }

    public void TransitionTo(MaintenanceRequestStatus status)
    {
        Status = status;
    }

    public void ApplyReviewedTriage(
        MaintenanceRequestCategory category,
        MaintenanceRequestPriority priority,
        string internalSummary)
    {
        if (string.IsNullOrWhiteSpace(internalSummary))
            throw new ArgumentException("Internal summary is required.", nameof(internalSummary));

        Category = category;
        Priority = priority;
        InternalSummary = internalSummary.Trim();
        AssignedTeam = ResolveAssignedTeam(category, priority);
        TargetResponseByUtc = SubmittedAtUtc.AddHours(priority switch
        {
            MaintenanceRequestPriority.Emergency => 1,
            MaintenanceRequestPriority.High => 4,
            MaintenanceRequestPriority.Normal => 12,
            _ => 24
        });
        Status = MaintenanceRequestStatus.InReview;
    }

    private static string BuildSummary(
        MaintenanceRequestCategory category,
        MaintenanceRequestPriority priority,
        string description) =>
        $"{priority} {category} request: {description.Trim()}";

    private static string ResolveAssignedTeam(
        MaintenanceRequestCategory category,
        MaintenanceRequestPriority priority) =>
        priority == MaintenanceRequestPriority.Emergency
            ? "Rapid Response Desk"
            : category switch
            {
                MaintenanceRequestCategory.Plumbing => "Plumbing Partners",
                MaintenanceRequestCategory.Electrical => "Electrical Services",
                MaintenanceRequestCategory.HVAC => "Climate Operations",
                MaintenanceRequestCategory.Appliances => "Appliance Care",
                MaintenanceRequestCategory.Security => "Security Response",
                _ => "General Maintenance"
            };
}
