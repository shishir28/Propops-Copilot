using System.Text.Json;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriagePreparationResultDto(
    string RulesVersion,
    MaintenanceTriageInputContractDto InputContract,
    MaintenanceTriageOutputContractDto OutputContractTemplate,
    JsonElement OutputContractSchema,
    IReadOnlyList<RetrievedKnowledgeItemDto> KnowledgeItems);
