using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class IntakeSubmissionRepository(PropOpsDbContext dbContext) : IIntakeSubmissionRepository
{
    public async Task AddWithRequestAsync(
        IntakeSubmission submission,
        MaintenanceRequest maintenanceRequest,
        CancellationToken cancellationToken = default)
    {
        await dbContext.MaintenanceRequests.AddAsync(maintenanceRequest, cancellationToken);
        await dbContext.IntakeSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntakeSubmission>> ListRecentAsync(
        int take = 12,
        CancellationToken cancellationToken = default) =>
        await dbContext.IntakeSubmissions
            .AsNoTracking()
            .Include(submission => submission.MaintenanceRequest)
            .OrderByDescending(submission => submission.ReceivedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}
