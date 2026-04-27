using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class MaintenanceRequestRepository(PropOpsDbContext dbContext) : IMaintenanceRequestRepository
{
    public async Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.MaintenanceRequests
            .AsNoTracking()
            .OrderByDescending(request => request.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceRequests.AddAsync(request, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.MaintenanceRequests.Update(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
