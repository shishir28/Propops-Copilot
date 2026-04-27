using System.Text;
using System.Text.Json;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Services;

public sealed class LearningDatasetService(IFineTuningExampleCandidateRepository candidateRepository)
{
    public async Task<IReadOnlyList<FineTuningExampleCandidateDto>> ListCandidatesAsync(
        CancellationToken cancellationToken = default)
    {
        var candidates = await candidateRepository.ListAsync(cancellationToken);
        return candidates.Select(Map).ToArray();
    }

    public async Task<FineTuningDatasetExportDto> ExportJsonlAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await candidateRepository.ListAsync(cancellationToken);
        var exportable = candidates
            .Where(candidate => candidate.Status is FineTuningCandidateStatus.Candidate or FineTuningCandidateStatus.Approved)
            .OrderBy(candidate => candidate.CreatedAtUtc)
            .ToArray();
        var builder = new StringBuilder();

        foreach (var candidate in exportable)
        {
            var row = new
            {
                input = JsonSerializer.Deserialize<JsonElement>(candidate.InputSnapshotJson),
                output = JsonSerializer.Deserialize<JsonElement>(candidate.OutputSnapshotJson),
                metadata = JsonSerializer.Deserialize<JsonElement>(candidate.MetadataSnapshotJson)
            };
            builder.AppendLine(JsonSerializer.Serialize(row, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }

        return new FineTuningDatasetExportDto(exportable.Length, builder.ToString());
    }

    private static FineTuningExampleCandidateDto Map(FineTuningExampleCandidate candidate) =>
        new(
            candidate.Id,
            candidate.MaintenanceRequestId,
            candidate.MaintenanceResolutionFeedbackId,
            candidate.Status,
            candidate.InputSnapshotJson,
            candidate.OutputSnapshotJson,
            candidate.MetadataSnapshotJson,
            candidate.ExclusionReason,
            candidate.CreatedAtUtc);
}
