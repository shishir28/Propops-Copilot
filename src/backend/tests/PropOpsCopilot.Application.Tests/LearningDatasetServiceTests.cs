using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Tests;

public sealed class LearningDatasetServiceTests
{
    [Fact]
    public async Task ExportJsonlAsync_ExportsOnlyCandidateOrApprovedExamples()
    {
        var repository = new InMemoryFineTuningExampleCandidateRepository(
            FineTuningExampleCandidate.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                FineTuningCandidateStatus.Candidate,
                """{"normalized_text":"Kitchen sink is leaking."}""",
                """{"category":"Plumbing","priority":"High"}""",
                """{"dispatch_outcome":"Completed"}""",
                string.Empty),
            FineTuningExampleCandidate.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                FineTuningCandidateStatus.Excluded,
                """{"normalized_text":"Duplicate request."}""",
                """{"category":"General","priority":"Normal"}""",
                """{"dispatch_outcome":"Duplicate"}""",
                "Duplicate request."));
        var service = new LearningDatasetService(repository);

        var export = await service.ExportJsonlAsync();

        Assert.Equal(1, export.ExampleCount);
        Assert.Contains("Kitchen sink is leaking", export.Jsonl);
        Assert.DoesNotContain("Duplicate request", export.Jsonl);
    }

    private sealed class InMemoryFineTuningExampleCandidateRepository(params FineTuningExampleCandidate[] candidates)
        : IFineTuningExampleCandidateRepository
    {
        public Task<IReadOnlyList<FineTuningExampleCandidate>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FineTuningExampleCandidate>>(candidates);

        public Task AddAsync(FineTuningExampleCandidate candidate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
