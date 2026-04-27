using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Domain.Entities;

public sealed class MaintenanceOperationalAction
{
    private MaintenanceOperationalAction()
    {
    }

    public Guid Id { get; private set; }

    public Guid MaintenanceRequestId { get; private set; }

    public MaintenanceRequest? MaintenanceRequest { get; private set; }

    public MaintenanceOperationalActionType ActionType { get; private set; }

    public string Detail { get; private set; } = string.Empty;

    public string ExternalReference { get; private set; } = string.Empty;

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static MaintenanceOperationalAction Create(
        Guid maintenanceRequestId,
        MaintenanceOperationalActionType actionType,
        string detail,
        string externalReference,
        string createdBy)
    {
        if (maintenanceRequestId == Guid.Empty)
            throw new ArgumentException("Maintenance request id is required.", nameof(maintenanceRequestId));

        if (string.IsNullOrWhiteSpace(detail))
            throw new ArgumentException("Action detail is required.", nameof(detail));

        return new MaintenanceOperationalAction
        {
            Id = Guid.NewGuid(),
            MaintenanceRequestId = maintenanceRequestId,
            ActionType = actionType,
            Detail = detail.Trim(),
            ExternalReference = externalReference.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "staff" : createdBy.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
