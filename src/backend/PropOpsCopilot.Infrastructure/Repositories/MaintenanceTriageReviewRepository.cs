using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class MaintenanceTriageReviewRepository(PropOpsDbContext dbContext) : IMaintenanceTriageReviewRepository
{
    public Task<MaintenanceTriageReview?> GetLatestForRequestAsync(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceTriageReviews
            .AsNoTracking()
            .Where(review => review.MaintenanceRequestId == maintenanceRequestId)
            .OrderByDescending(review => review.ReviewedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(MaintenanceTriageReview review, CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceTriageReviews.AddAsync(review, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
