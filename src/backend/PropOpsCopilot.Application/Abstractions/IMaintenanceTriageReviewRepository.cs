using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IMaintenanceTriageReviewRepository
{
    Task<MaintenanceTriageReview?> GetLatestForRequestAsync(Guid maintenanceRequestId, CancellationToken cancellationToken = default);

    Task AddAsync(MaintenanceTriageReview review, CancellationToken cancellationToken = default);
}
