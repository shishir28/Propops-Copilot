using System.Text.Json;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageInferenceResultDto(
    string RulesVersion,
    MaintenanceTriageInputContractDto InputContract,
    MaintenanceTriageOutputContractDto OutputContract,
    JsonElement OutputContractSchema,
    IReadOnlyList<RetrievedKnowledgeItemDto> KnowledgeItems,
    MaintenanceTriageGuardrailResultDto Guardrails,
    MaintenanceTriageInferenceMetadataDto InferenceMetadata);
