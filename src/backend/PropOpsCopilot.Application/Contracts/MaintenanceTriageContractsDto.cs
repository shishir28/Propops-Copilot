using System.Text.Json;

namespace PropOpsCopilot.Application.Contracts;

public sealed record MaintenanceTriageContractsDto(
    string RulesVersion,
    JsonElement InputContractSchema,
    JsonElement OutputContractSchema,
    MaintenanceTriageOutputContractDto OutputContractTemplate);
