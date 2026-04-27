using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IMaintenanceResolutionFeedbackRepository
{
    Task<MaintenanceResolutionFeedback?> GetLatestForRequestAsync(Guid maintenanceRequestId, CancellationToken cancellationToken = default);

    Task AddAsync(MaintenanceResolutionFeedback feedback, CancellationToken cancellationToken = default);
}
