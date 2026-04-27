using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Services;

public sealed class MaintenanceOperationsService(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IMaintenanceTriageReviewRepository triageReviewRepository,
    IMaintenanceOperationalActionRepository operationalActionRepository)
{
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
        var actions = await operationalActionRepository.ListForRequestAsync(request.Id, cancellationToken);

        return new MaintenanceOperationsDetailDto(
            MaintenanceRequestService.Map(request),
            latestReview is null ? null : Map(latestReview),
            actions.Select(Map).ToArray());
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
}
