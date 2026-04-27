using System.Text.Json;
using System.Text.Json.Serialization;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Services;

public sealed class MaintenanceOperationsService(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IMaintenanceTriageReviewRepository triageReviewRepository,
    IMaintenanceOperationalActionRepository operationalActionRepository,
    IMaintenanceResolutionFeedbackRepository resolutionFeedbackRepository,
    IFineTuningExampleCandidateRepository fineTuningExampleCandidateRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<MaintenanceOperationsDetailDto> GetDetailAsync(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRequestOrThrowAsync(maintenanceRequestId, cancellationToken);
        return await BuildDetailAsync(request, cancellationToken);
    }

    public async Task<MaintenanceOperationsDetailDto> SubmitTriageReviewAsync(
        Guid maintenanceRequestId,
        SubmitMaintenanceTriageReviewCommand command,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRequestOrThrowAsync(maintenanceRequestId, cancellationToken);
        var aiCategory = ParseEnum<MaintenanceRequestCategory>(command.AiOutput.Category, "AI category");
        var aiPriority = ParseEnum<MaintenanceRequestPriority>(command.AiOutput.Priority, "AI priority");
        var guardrailSummary = string.Join("; ", command.Guardrails.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));

        var review = MaintenanceTriageReview.Create(
            maintenanceRequestId,
            aiCategory,
            aiPriority,
            command.AiOutput.VendorType,
            command.AiOutput.DispatchDecision,
            command.AiOutput.InternalSummary,
            command.AiOutput.TenantResponseDraft,
            command.Category,
            command.Priority,
            command.VendorType,
            command.DispatchDecision,
            command.InternalSummary,
            command.TenantResponseDraft,
            command.Guardrails.RequiresHumanReview,
            guardrailSummary,
            reviewedBy);

        request.ApplyReviewedTriage(command.Category, command.Priority, command.InternalSummary);

        await triageReviewRepository.AddAsync(review, cancellationToken);
        await maintenanceRequestRepository.UpdateAsync(request, cancellationToken);

        return await BuildDetailAsync(request, cancellationToken);
    }

    public Task<MaintenanceOperationsDetailDto> CreateWorkOrderAsync(
        Guid maintenanceRequestId,
        CreateWorkOrderCommand command,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var summary = string.IsNullOrWhiteSpace(command.Summary)
            ? "Create work order from approved maintenance triage."
            : command.Summary;
        var externalReference = $"WO-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28].ToUpperInvariant();

        return AddActionAsync(
            maintenanceRequestId,
            MaintenanceOperationalActionType.WorkOrderCreated,
            summary,
            externalReference,
            createdBy,
            MaintenanceRequestStatus.Scheduled,
            cancellationToken);
    }

    public Task<MaintenanceOperationsDetailDto> AssignVendorAsync(
        Guid maintenanceRequestId,
        AssignVendorCommand command,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.VendorName))
            throw new ArgumentException("Vendor name is required.", nameof(command));

        return AddActionAsync(
            maintenanceRequestId,
            MaintenanceOperationalActionType.VendorAssigned,
            $"Assigned vendor: {command.VendorName.Trim()}",
            string.Empty,
            createdBy,
            MaintenanceRequestStatus.Scheduled,
            cancellationToken);
    }

    public Task<MaintenanceOperationsDetailDto> NotifyTenantAsync(
        Guid maintenanceRequestId,
        NotifyTenantCommand command,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Message))
            throw new ArgumentException("Tenant notification message is required.", nameof(command));

        return AddActionAsync(
            maintenanceRequestId,
            MaintenanceOperationalActionType.TenantNotified,
            command.Message,
            string.Empty,
            createdBy,
            null,
            cancellationToken);
    }

    public Task<MaintenanceOperationsDetailDto> LogInternalNoteAsync(
        Guid maintenanceRequestId,
        LogInternalNoteCommand command,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Note))
            throw new ArgumentException("Internal note is required.", nameof(command));

        return AddActionAsync(
            maintenanceRequestId,
            MaintenanceOperationalActionType.InternalNoteLogged,
            command.Note,
            string.Empty,
            createdBy,
            null,
            cancellationToken);
    }

    public async Task<MaintenanceOperationsDetailDto> SubmitResolutionFeedbackAsync(
        Guid maintenanceRequestId,
        SubmitMaintenanceResolutionFeedbackCommand command,
        string resolvedBy,
        CancellationToken cancellationToken = default)
    {
        var request = await GetRequestOrThrowAsync(maintenanceRequestId, cancellationToken);
        var latestReview = await triageReviewRepository.GetLatestForRequestAsync(request.Id, cancellationToken);
        var feedback = MaintenanceResolutionFeedback.Create(
            request.Id,
            latestReview?.Id,
            command.FinalResolution,
            command.CorrectedCategory,
            command.CorrectedPriority,
            command.FinalTenantResponse,
            command.DispatchOutcome,
            command.ResolutionNotes,
            command.ExcludeFromTraining,
            command.ExclusionReason,
            resolvedBy);

        await resolutionFeedbackRepository.AddAsync(feedback, cancellationToken);

        request.ApplyReviewedTriage(command.CorrectedCategory, command.CorrectedPriority, command.FinalResolution);
        request.TransitionTo(command.DispatchOutcome is MaintenanceDispatchOutcome.Cancelled or MaintenanceDispatchOutcome.Duplicate
            ? MaintenanceRequestStatus.Cancelled
            : MaintenanceRequestStatus.Completed);
        await maintenanceRequestRepository.UpdateAsync(request, cancellationToken);

        var actions = await operationalActionRepository.ListForRequestAsync(request.Id, cancellationToken);
        var candidate = BuildFineTuningCandidate(request, latestReview, feedback, actions);
        await fineTuningExampleCandidateRepository.AddAsync(candidate, cancellationToken);

        return await BuildDetailAsync(request, cancellationToken);
    }

    private async Task<MaintenanceOperationsDetailDto> AddActionAsync(
        Guid maintenanceRequestId,
        MaintenanceOperationalActionType actionType,
        string detail,
        string externalReference,
        string createdBy,
        MaintenanceRequestStatus? nextStatus,
        CancellationToken cancellationToken)
    {
        var request = await GetRequestOrThrowAsync(maintenanceRequestId, cancellationToken);
        var action = MaintenanceOperationalAction.Create(maintenanceRequestId, actionType, detail, externalReference, createdBy);

        await operationalActionRepository.AddAsync(action, cancellationToken);
        if (nextStatus is not null)
        {
            request.TransitionTo(nextStatus.Value);
            await maintenanceRequestRepository.UpdateAsync(request, cancellationToken);
        }

        return await BuildDetailAsync(request, cancellationToken);
    }

    private async Task<MaintenanceRequest> GetRequestOrThrowAsync(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken)
    {
        if (maintenanceRequestId == Guid.Empty)
            throw new ArgumentException("Maintenance request id is required.", nameof(maintenanceRequestId));

        return await maintenanceRequestRepository.GetByIdAsync(maintenanceRequestId, cancellationToken)
               ?? throw new KeyNotFoundException($"Maintenance request '{maintenanceRequestId}' was not found.");
    }

    private async Task<MaintenanceOperationsDetailDto> BuildDetailAsync(
        MaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var latestReview = await triageReviewRepository.GetLatestForRequestAsync(request.Id, cancellationToken);
        var latestFeedback = await resolutionFeedbackRepository.GetLatestForRequestAsync(request.Id, cancellationToken);
        var actions = await operationalActionRepository.ListForRequestAsync(request.Id, cancellationToken);

        return new MaintenanceOperationsDetailDto(
            MaintenanceRequestService.Map(request),
            latestReview is null ? null : Map(latestReview),
            latestFeedback is null ? null : Map(latestFeedback),
            actions.Select(Map).ToArray());
    }

    private static FineTuningExampleCandidate BuildFineTuningCandidate(
        MaintenanceRequest request,
        MaintenanceTriageReview? latestReview,
        MaintenanceResolutionFeedback feedback,
        IReadOnlyList<MaintenanceOperationalAction> actions)
    {
        var eligibilityProblems = new List<string>();
        if (latestReview is null)
            eligibilityProblems.Add("Missing staff triage review.");
        if (feedback.ExcludeFromTraining)
            eligibilityProblems.Add(feedback.ExclusionReason);
        if (feedback.DispatchOutcome is MaintenanceDispatchOutcome.Duplicate or MaintenanceDispatchOutcome.Cancelled or MaintenanceDispatchOutcome.NotMaintenance)
            eligibilityProblems.Add($"Dispatch outcome '{feedback.DispatchOutcome}' is not suitable for training.");
        if (!actions.Any(action => action.ActionType is MaintenanceOperationalActionType.WorkOrderCreated or MaintenanceOperationalActionType.VendorAssigned))
            eligibilityProblems.Add("Missing work-order or vendor action evidence.");

        var status = eligibilityProblems.Count == 0
            ? FineTuningCandidateStatus.Candidate
            : FineTuningCandidateStatus.Excluded;

        var inputSnapshot = new
        {
            request_id = request.Id,
            reference_number = request.ReferenceNumber,
            normalized_text = request.Description,
            property_name = request.PropertyName,
            unit_number = string.IsNullOrWhiteSpace(request.UnitNumber) ? null : request.UnitNumber,
            channel = request.Channel.ToString(),
            category_hint = latestReview?.AiCategory.ToString() ?? request.Category.ToString(),
            priority_hint = latestReview?.AiPriority.ToString() ?? request.Priority.ToString(),
            submitted_at_utc = request.SubmittedAtUtc
        };
        var outputSnapshot = new
        {
            category = feedback.CorrectedCategory.ToString(),
            priority = feedback.CorrectedPriority.ToString(),
            vendor_type = latestReview?.FinalVendorType ?? request.AssignedTeam,
            dispatch_decision = latestReview?.FinalDispatchDecision ?? feedback.FinalResolution,
            internal_summary = feedback.FinalResolution,
            tenant_response_draft = feedback.FinalTenantResponse
        };
        var metadataSnapshot = new
        {
            source_request_id = request.Id,
            review_id = latestReview?.Id,
            feedback_id = feedback.Id,
            review_status = latestReview?.Status.ToString(),
            dispatch_outcome = feedback.DispatchOutcome.ToString(),
            action_count = actions.Count,
            generated_from = "level5-feedback"
        };

        return FineTuningExampleCandidate.Create(
            request.Id,
            feedback.Id,
            status,
            JsonSerializer.Serialize(inputSnapshot, JsonOptions),
            JsonSerializer.Serialize(outputSnapshot, JsonOptions),
            JsonSerializer.Serialize(metadataSnapshot, JsonOptions),
            string.Join("; ", eligibilityProblems.Where(problem => !string.IsNullOrWhiteSpace(problem))));
    }

    private static T ParseEnum<T>(string value, string fieldName)
        where T : struct
    {
        if (Enum.TryParse<T>(value, ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException($"{fieldName} '{value}' is not supported.");
    }

    private static MaintenanceTriageReviewDto Map(MaintenanceTriageReview review) =>
        new(
            review.Id,
            review.MaintenanceRequestId,
            review.AiCategory,
            review.AiPriority,
            review.AiVendorType,
            review.AiDispatchDecision,
            review.AiInternalSummary,
            review.AiTenantResponseDraft,
            review.FinalCategory,
            review.FinalPriority,
            review.FinalVendorType,
            review.FinalDispatchDecision,
            review.FinalInternalSummary,
            review.FinalTenantResponseDraft,
            review.GuardrailRequiresHumanReview,
            review.GuardrailSummary,
            review.Status,
            review.ReviewedBy,
            review.ReviewedAtUtc);

    private static MaintenanceOperationalActionDto Map(MaintenanceOperationalAction action) =>
        new(
            action.Id,
            action.MaintenanceRequestId,
            action.ActionType,
            action.Detail,
            action.ExternalReference,
            action.CreatedBy,
            action.CreatedAtUtc);

    private static MaintenanceResolutionFeedbackDto Map(MaintenanceResolutionFeedback feedback) =>
        new(
            feedback.Id,
            feedback.MaintenanceRequestId,
            feedback.MaintenanceTriageReviewId,
            feedback.FinalResolution,
            feedback.CorrectedCategory,
            feedback.CorrectedPriority,
            feedback.FinalTenantResponse,
            feedback.DispatchOutcome,
            feedback.ResolutionNotes,
            feedback.ExcludeFromTraining,
            feedback.ExclusionReason,
            feedback.ResolvedBy,
            feedback.ResolvedAtUtc);
}
