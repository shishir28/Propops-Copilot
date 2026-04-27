using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Options;

namespace PropOpsCopilot.Application.Services;

public sealed class MaintenanceTriagePreparationService(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IOptions<IntakePreprocessingOptions> preprocessingOptions,
    HttpClient aiClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IntakePreprocessingOptions preprocessingOptions = preprocessingOptions.Value;

    public async Task<MaintenanceTriageContractsDto> GetContractsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await aiClient.GetAsync("/v1/maintenance/contracts", cancellationToken);
        var payload = await ReadResponseAsync<PythonContractsResponse>(response, "retrieve Level 2 AI contracts", cancellationToken);

        return new MaintenanceTriageContractsDto(
            payload.RulesVersion,
            payload.InputContractSchema,
            payload.OutputContractSchema,
            Map(payload.OutputContractTemplate));
    }

    public async Task<MaintenanceTriagePreparationResultDto> PrepareAsync(
        PrepareMaintenanceTriageCommand command,
        CancellationToken cancellationToken = default)
    {
        var payload = await BuildInputPayloadAsync(command.MaintenanceRequestId, cancellationToken);

        using var response = await aiClient.PostAsJsonAsync(
            "/v1/maintenance/triage/prepare",
            payload,
            JsonOptions,
            cancellationToken);

        var prepared = await ReadResponseAsync<PythonPreparationResponse>(response, "prepare Level 2 triage context", cancellationToken);

        return new MaintenanceTriagePreparationResultDto(
            prepared.RulesVersion,
            Map(prepared.InputContract),
            Map(prepared.OutputContractTemplate),
            prepared.OutputContractSchema,
            [.. prepared.KnowledgeItems.Select(Map)]);
    }

    public async Task<MaintenanceTriageInferenceResultDto> InferAsync(
        PrepareMaintenanceTriageCommand command,
        CancellationToken cancellationToken = default)
    {
        var payload = await BuildInputPayloadAsync(command.MaintenanceRequestId, cancellationToken);

        using var response = await aiClient.PostAsJsonAsync(
            "/v1/maintenance/triage/infer",
            payload,
            JsonOptions,
            cancellationToken);

        var inferred = await ReadResponseAsync<PythonInferenceResponse>(response, "run Level 3 triage inference", cancellationToken);

        return new MaintenanceTriageInferenceResultDto(
            inferred.RulesVersion,
            Map(inferred.InputContract),
            Map(inferred.OutputContract),
            inferred.OutputContractSchema,
            [.. inferred.KnowledgeItems.Select(Map)],
            Map(inferred.Guardrails),
            Map(inferred.InferenceMetadata));
    }

    private async Task<PythonInputContractRequest> BuildInputPayloadAsync(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken)
    {
        if (maintenanceRequestId == Guid.Empty)
            throw new ArgumentException("A maintenance request id is required.", nameof(maintenanceRequestId));

        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(maintenanceRequestId, cancellationToken);
        if (maintenanceRequest is null)
            throw new KeyNotFoundException($"Maintenance request '{maintenanceRequestId}' was not found.");

        return new PythonInputContractRequest(
            maintenanceRequest.Id.ToString(),
            maintenanceRequest.ReferenceNumber,
            maintenanceRequest.Description,
            maintenanceRequest.PropertyName,
            string.IsNullOrWhiteSpace(maintenanceRequest.UnitNumber) ? null : maintenanceRequest.UnitNumber,
            maintenanceRequest.Channel.ToString(),
            maintenanceRequest.Category.ToString(),
            maintenanceRequest.Priority.ToString(),
            DetectAfterHours(maintenanceRequest.SubmittedAtUtc),
            maintenanceRequest.SubmittedAtUtc);
    }

    private bool DetectAfterHours(DateTimeOffset submittedAtUtc)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(preprocessingOptions.TimeZoneId);
        var localTime = TimeZoneInfo.ConvertTime(submittedAtUtc, timeZone);

        return localTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
               || localTime.Hour < preprocessingOptions.BusinessHoursStartHour
               || localTime.Hour >= preprocessingOptions.BusinessHoursEndHour;
    }

    private static async Task<T> ReadResponseAsync<T>(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            var title = response.StatusCode == HttpStatusCode.BadRequest ? "AI contract rejected" : "AI service failure";
            throw new InvalidOperationException($"{title} while attempting to {operation}: {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException($"The AI service returned an empty payload when attempting to {operation}.");
    }

    private static MaintenanceTriageInputContractDto Map(PythonInputContract input) =>
        new(
            input.RequestId,
            input.ReferenceNumber,
            input.NormalizedText,
            input.PropertyName,
            input.UnitNumber,
            input.Channel,
            input.CategoryHint,
            input.PriorityHint,
            input.IsAfterHours,
            input.SubmittedAtUtc);

    private static MaintenanceTriageOutputContractDto Map(PythonOutputContract output) =>
        new(
            output.Category,
            output.Priority,
            output.VendorType,
            output.DispatchDecision,
            output.InternalSummary,
            output.TenantResponseDraft);

    private static RetrievedKnowledgeItemDto Map(PythonKnowledgeItem item) =>
        new(
            item.SourceType,
            item.Key,
            item.Title,
            item.Content,
            item.Rationale);

    private static MaintenanceTriageGuardrailResultDto Map(PythonGuardrailResult guardrails) =>
        new(
            guardrails.SchemaValid,
            guardrails.PolicyPassed,
            guardrails.EmergencyKeywordCheckPassed,
            guardrails.ConfidenceScore,
            guardrails.ConfidenceThreshold,
            guardrails.RequiresHumanReview,
            guardrails.FallbackApplied,
            [.. guardrails.Issues.Select(Map)]);

    private static MaintenanceTriageGuardrailIssueDto Map(PythonGuardrailIssue issue) =>
        new(
            issue.Code,
            issue.Severity,
            issue.Message);

    private static MaintenanceTriageInferenceMetadataDto Map(PythonInferenceMetadata metadata) =>
        new(
            metadata.ProviderMode,
            metadata.ModelName);

    private sealed record PythonContractsResponse(
        [property: JsonPropertyName("rules_version")] string RulesVersion,
        [property: JsonPropertyName("input_contract_schema")] JsonElement InputContractSchema,
        [property: JsonPropertyName("output_contract_schema")] JsonElement OutputContractSchema,
        [property: JsonPropertyName("output_contract_template")] PythonOutputContract OutputContractTemplate);

    private sealed record PythonPreparationResponse(
        [property: JsonPropertyName("rules_version")] string RulesVersion,
        [property: JsonPropertyName("input_contract")] PythonInputContract InputContract,
        [property: JsonPropertyName("output_contract_template")] PythonOutputContract OutputContractTemplate,
        [property: JsonPropertyName("output_contract_schema")] JsonElement OutputContractSchema,
        [property: JsonPropertyName("knowledge_items")] IReadOnlyList<PythonKnowledgeItem> KnowledgeItems);

    private sealed record PythonInferenceResponse(
        [property: JsonPropertyName("rules_version")] string RulesVersion,
        [property: JsonPropertyName("input_contract")] PythonInputContract InputContract,
        [property: JsonPropertyName("output_contract")] PythonOutputContract OutputContract,
        [property: JsonPropertyName("output_contract_schema")] JsonElement OutputContractSchema,
        [property: JsonPropertyName("knowledge_items")] IReadOnlyList<PythonKnowledgeItem> KnowledgeItems,
        [property: JsonPropertyName("guardrails")] PythonGuardrailResult Guardrails,
        [property: JsonPropertyName("inference_metadata")] PythonInferenceMetadata InferenceMetadata);

    private sealed record PythonInputContractRequest(
        [property: JsonPropertyName("request_id")] string RequestId,
        [property: JsonPropertyName("reference_number")] string ReferenceNumber,
        [property: JsonPropertyName("normalized_text")] string NormalizedText,
        [property: JsonPropertyName("property_name")] string PropertyName,
        [property: JsonPropertyName("unit_number")] string? UnitNumber,
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("category_hint")] string? CategoryHint,
        [property: JsonPropertyName("priority_hint")] string? PriorityHint,
        [property: JsonPropertyName("is_after_hours")] bool IsAfterHours,
        [property: JsonPropertyName("submitted_at_utc")] DateTimeOffset SubmittedAtUtc);

    private sealed record PythonInputContract(
        [property: JsonPropertyName("request_id")] string RequestId,
        [property: JsonPropertyName("reference_number")] string ReferenceNumber,
        [property: JsonPropertyName("normalized_text")] string NormalizedText,
        [property: JsonPropertyName("property_name")] string PropertyName,
        [property: JsonPropertyName("unit_number")] string? UnitNumber,
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("category_hint")] string? CategoryHint,
        [property: JsonPropertyName("priority_hint")] string? PriorityHint,
        [property: JsonPropertyName("is_after_hours")] bool IsAfterHours,
        [property: JsonPropertyName("submitted_at_utc")] DateTimeOffset SubmittedAtUtc);

    private sealed record PythonOutputContract(
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("vendor_type")] string VendorType,
        [property: JsonPropertyName("dispatch_decision")] string DispatchDecision,
        [property: JsonPropertyName("internal_summary")] string InternalSummary,
        [property: JsonPropertyName("tenant_response_draft")] string TenantResponseDraft);

    private sealed record PythonKnowledgeItem(
        [property: JsonPropertyName("source_type")] string SourceType,
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("rationale")] string Rationale);

    private sealed record PythonGuardrailResult(
        [property: JsonPropertyName("schema_valid")] bool SchemaValid,
        [property: JsonPropertyName("policy_passed")] bool PolicyPassed,
        [property: JsonPropertyName("emergency_keyword_check_passed")] bool EmergencyKeywordCheckPassed,
        [property: JsonPropertyName("confidence_score")] double ConfidenceScore,
        [property: JsonPropertyName("confidence_threshold")] double ConfidenceThreshold,
        [property: JsonPropertyName("requires_human_review")] bool RequiresHumanReview,
        [property: JsonPropertyName("fallback_applied")] bool FallbackApplied,
        [property: JsonPropertyName("issues")] IReadOnlyList<PythonGuardrailIssue> Issues);

    private sealed record PythonGuardrailIssue(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("message")] string Message);

    private sealed record PythonInferenceMetadata(
        [property: JsonPropertyName("provider_mode")] string ProviderMode,
        [property: JsonPropertyName("model_name")] string ModelName);
}
