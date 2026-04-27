using System.Net;
using System.Net.Http.Json;
using System.Text;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Options;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Tests;

public sealed class MaintenanceTriagePreparationServiceTests
{
    [Fact]
    public async Task GetContractsAsync_MapsContractsReturnedByTheAiService()
    {
        var service = CreateService(
            new StubMaintenanceRequestRepository(null),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    rules_version = "2026.04-level2",
                    input_contract_schema = new { type = "object" },
                    output_contract_schema = new { type = "object" },
                    output_contract_template = new
                    {
                        category = "General",
                        priority = "Normal",
                        vendor_type = "General Maintenance Contractor",
                        dispatch_decision = "Queue for standard triage and assign during business hours.",
                        internal_summary = "General issue at Example Property.",
                        tenant_response_draft = "Acknowledged."
                    }
                })
            });

        var result = await service.GetContractsAsync();

        Assert.Equal("2026.04-level2", result.RulesVersion);
        Assert.Equal("object", result.InputContractSchema.GetProperty("type").GetString());
        Assert.Equal("General Maintenance Contractor", result.OutputContractTemplate.VendorType);
    }

    [Fact]
    public async Task PrepareAsync_SendsMappedRequestPayloadAndReturnsKnowledgeItems()
    {
        var maintenanceRequest = MaintenanceRequest.Create(
            "Jordan Blake",
            "manager@propops.local",
            "0412200100",
            "Harbour View Residences",
            "22A",
            "Front door lock is jammed and the apartment cannot secure properly.",
            MaintenanceRequestCategory.Security,
            MaintenanceRequestPriority.Emergency,
            IntakeChannel.Portal);
        HttpRequestMessage? capturedRequest = null;
        var service = CreateService(
            new StubMaintenanceRequestRepository(maintenanceRequest),
            request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        rules_version = "2026.04-level2",
                        input_contract = new
                        {
                            request_id = maintenanceRequest.Id.ToString(),
                            reference_number = maintenanceRequest.ReferenceNumber,
                            normalized_text = maintenanceRequest.Description,
                            property_name = maintenanceRequest.PropertyName,
                            unit_number = maintenanceRequest.UnitNumber,
                            channel = maintenanceRequest.Channel.ToString(),
                            category_hint = maintenanceRequest.Category.ToString(),
                            priority_hint = maintenanceRequest.Priority.ToString(),
                            is_after_hours = false,
                            submitted_at_utc = maintenanceRequest.SubmittedAtUtc
                        },
                        output_contract_template = new
                        {
                            category = "Security",
                            priority = "Emergency",
                            vendor_type = "Emergency Locksmith / Security Contractor",
                            dispatch_decision = "Dispatch immediately to the emergency on-call vendor and notify the resident.",
                            internal_summary = "Emergency security issue at Harbour View Residences / 22A.",
                            tenant_response_draft = "We are dispatching the on-call locksmith now."
                        },
                        output_contract_schema = new { type = "object" },
                        knowledge_items = new[]
                        {
                            new
                            {
                                source_type = "EmergencyPolicy",
                                key = "emergency-policy",
                                title = "Emergency maintenance policy",
                                content = "Policy content",
                                rationale = "Emergency routing attached."
                            }
                        }
                    })
                };
            });

        var result = await service.PrepareAsync(new PrepareMaintenanceTriageCommand(maintenanceRequest.Id));

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/v1/maintenance/triage/prepare", capturedRequest.RequestUri!.AbsolutePath);
        var requestBody = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains(maintenanceRequest.Id.ToString(), requestBody);
        Assert.Contains(maintenanceRequest.ReferenceNumber, requestBody);
        Assert.Equal("2026.04-level2", result.RulesVersion);
        Assert.Equal("Emergency Locksmith / Security Contractor", result.OutputContractTemplate.VendorType);
        Assert.Single(result.KnowledgeItems);
        Assert.Equal("EmergencyPolicy", result.KnowledgeItems[0].SourceType);
    }

    [Fact]
    public async Task InferAsync_MapsGuardrailsAndInferenceMetadata()
    {
        var maintenanceRequest = MaintenanceRequest.Create(
            "Jordan Blake",
            "manager@propops.local",
            "0412200100",
            "Harbour View Residences",
            "22A",
            "Kitchen sink pipe is leaking heavily under the cabinet.",
            MaintenanceRequestCategory.Plumbing,
            MaintenanceRequestPriority.High,
            IntakeChannel.Portal);
        var service = CreateService(
            new StubMaintenanceRequestRepository(maintenanceRequest),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    rules_version = "2026.04-level2",
                    input_contract = new
                    {
                        request_id = maintenanceRequest.Id.ToString(),
                        reference_number = maintenanceRequest.ReferenceNumber,
                        normalized_text = maintenanceRequest.Description,
                        property_name = maintenanceRequest.PropertyName,
                        unit_number = maintenanceRequest.UnitNumber,
                        channel = maintenanceRequest.Channel.ToString(),
                        category_hint = maintenanceRequest.Category.ToString(),
                        priority_hint = maintenanceRequest.Priority.ToString(),
                        is_after_hours = false,
                        submitted_at_utc = maintenanceRequest.SubmittedAtUtc
                    },
                    output_contract = new
                    {
                        category = "Plumbing",
                        priority = "High",
                        vendor_type = "Licensed Plumber",
                        dispatch_decision = "Create an urgent work order and assign the preferred vendor next.",
                        internal_summary = "High Plumbing issue at Harbour View Residences / 22A.",
                        tenant_response_draft = "We have logged the request as high priority."
                    },
                    output_contract_schema = new { type = "object" },
                    knowledge_items = Array.Empty<object>(),
                    guardrails = new
                    {
                        schema_valid = true,
                        policy_passed = true,
                        emergency_keyword_check_passed = true,
                        confidence_score = 0.82,
                        confidence_threshold = 0.68,
                        requires_human_review = false,
                        fallback_applied = false,
                        issues = Array.Empty<object>()
                    },
                    inference_metadata = new
                    {
                        provider_mode = "heuristic",
                        model_name = "Qwen/Qwen2.5-7B-Instruct"
                    }
                })
            });

        var result = await service.InferAsync(new PrepareMaintenanceTriageCommand(maintenanceRequest.Id));

        Assert.Equal("Plumbing", result.OutputContract.Category);
        Assert.True(result.Guardrails.SchemaValid);
        Assert.False(result.Guardrails.RequiresHumanReview);
        Assert.Equal("heuristic", result.InferenceMetadata.ProviderMode);
        Assert.Equal("Qwen/Qwen2.5-7B-Instruct", result.InferenceMetadata.ModelName);
    }

    [Fact]
    public async Task PrepareAsync_ThrowsWhenTheMaintenanceRequestIdIsMissing()
    {
        var service = CreateService(
            new StubMaintenanceRequestRepository(null),
            _ => new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.ThrowsAsync<ArgumentException>(() => service.PrepareAsync(new PrepareMaintenanceTriageCommand(Guid.Empty)));
    }

    [Fact]
    public async Task GetContractsAsync_ThrowsWhenTheAiServiceReturnsAnError()
    {
        var service = CreateService(
            new StubMaintenanceRequestRepository(null),
            _ => new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("ai unavailable", Encoding.UTF8, "text/plain")
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetContractsAsync());

        Assert.Contains("AI service failure", exception.Message);
    }

    private static MaintenanceTriagePreparationService CreateService(
        IMaintenanceRequestRepository repository,
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IntakePreprocessingOptions
        {
            TimeZoneId = "UTC",
            BusinessHoursStartHour = 8,
            BusinessHoursEndHour = 18
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("http://ai-service:8000")
        };

        return new MaintenanceTriagePreparationService(repository, options, httpClient);
    }

    private sealed class StubMaintenanceRequestRepository(MaintenanceRequest? maintenanceRequest) : IMaintenanceRequestRepository
    {
        public Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MaintenanceRequest>>(maintenanceRequest is null ? [] : [maintenanceRequest]);

        public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(maintenanceRequest?.Id == id ? maintenanceRequest : null);

        public Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
