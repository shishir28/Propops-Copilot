using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IIntakeSubmissionRepository
{
    Task AddWithRequestAsync(
        IntakeSubmission submission,
        MaintenanceRequest maintenanceRequest,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntakeSubmission>> ListRecentAsync(int take = 12, CancellationToken cancellationToken = default);
}
