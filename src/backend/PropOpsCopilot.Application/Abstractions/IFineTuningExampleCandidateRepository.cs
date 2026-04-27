using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IFineTuningExampleCandidateRepository
{
    Task<IReadOnlyList<FineTuningExampleCandidate>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FineTuningExampleCandidate candidate, CancellationToken cancellationToken = default);
}
