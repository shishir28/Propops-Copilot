using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class MaintenanceOperationalActionRepository(PropOpsDbContext dbContext) : IMaintenanceOperationalActionRepository
{
    public async Task<IReadOnlyList<MaintenanceOperationalAction>> ListForRequestAsync(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken = default) =>
        await dbContext.MaintenanceOperationalActions
            .AsNoTracking()
            .Where(action => action.MaintenanceRequestId == maintenanceRequestId)
            .OrderByDescending(action => action.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MaintenanceOperationalAction action, CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceOperationalActions.AddAsync(action, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
