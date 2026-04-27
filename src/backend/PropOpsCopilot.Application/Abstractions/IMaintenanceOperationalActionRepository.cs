using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IMaintenanceOperationalActionRepository
{
    Task<IReadOnlyList<MaintenanceOperationalAction>> ListForRequestAsync(Guid maintenanceRequestId, CancellationToken cancellationToken = default);

    Task AddAsync(MaintenanceOperationalAction action, CancellationToken cancellationToken = default);
}
