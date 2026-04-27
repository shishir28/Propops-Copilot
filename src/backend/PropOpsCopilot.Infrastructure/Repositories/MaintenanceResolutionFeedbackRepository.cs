using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class MaintenanceResolutionFeedbackRepository(PropOpsDbContext dbContext)
    : IMaintenanceResolutionFeedbackRepository
{
    public Task<MaintenanceResolutionFeedback?> GetLatestForRequestAsync(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceResolutionFeedback
            .AsNoTracking()
            .Where(feedback => feedback.MaintenanceRequestId == maintenanceRequestId)
            .OrderByDescending(feedback => feedback.ResolvedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(MaintenanceResolutionFeedback feedback, CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceResolutionFeedback.AddAsync(feedback, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
