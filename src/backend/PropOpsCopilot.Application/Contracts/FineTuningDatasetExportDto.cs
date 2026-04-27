namespace PropOpsCopilot.Application.Contracts;

public sealed record FineTuningDatasetExportDto(
    int ExampleCount,
    string Jsonl);
