using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class FineTuningExampleCandidateRepository(PropOpsDbContext dbContext)
    : IFineTuningExampleCandidateRepository
{
    public async Task<IReadOnlyList<FineTuningExampleCandidate>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.FineTuningExampleCandidates
            .AsNoTracking()
            .OrderByDescending(candidate => candidate.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FineTuningExampleCandidate candidate, CancellationToken cancellationToken = default)
    {
        await dbContext.FineTuningExampleCandidates.AddAsync(candidate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
