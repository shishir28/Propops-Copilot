using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IMaintenanceRequestRepository
{
    Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default);
}
