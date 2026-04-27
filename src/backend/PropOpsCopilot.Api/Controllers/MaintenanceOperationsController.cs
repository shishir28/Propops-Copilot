using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Api.Controllers;

[ApiController]
[Route("api/maintenanceRequests/{maintenanceRequestId:guid}/operations")]
[Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher}")]
public sealed class MaintenanceOperationsController(MaintenanceOperationsService maintenanceOperationsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceOperationsDetailDto>> GetDetail(
        Guid maintenanceRequestId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await maintenanceOperationsService.GetDetailAsync(maintenanceRequestId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Maintenance request not found",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("triage-review")]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceOperationsDetailDto>> SubmitTriageReview(
        Guid maintenanceRequestId,
        [FromBody] SubmitMaintenanceTriageReviewCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var reviewedBy = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "staff";
            return Ok(await maintenanceOperationsService.SubmitTriageReviewAsync(
                maintenanceRequestId,
                command,
                reviewedBy,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid triage review",
                Detail = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Maintenance request not found",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("actions/work-order")]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<MaintenanceOperationsDetailDto>> CreateWorkOrder(
        Guid maintenanceRequestId,
        [FromBody] CreateWorkOrderCommand command,
        CancellationToken cancellationToken) =>
        RunActionAsync((createdBy, token) => maintenanceOperationsService.CreateWorkOrderAsync(
            maintenanceRequestId,
            command,
            createdBy,
            token), cancellationToken);

    [HttpPost("actions/vendor-assignment")]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<MaintenanceOperationsDetailDto>> AssignVendor(
        Guid maintenanceRequestId,
        [FromBody] AssignVendorCommand command,
        CancellationToken cancellationToken) =>
        RunActionAsync((createdBy, token) => maintenanceOperationsService.AssignVendorAsync(
            maintenanceRequestId,
            command,
            createdBy,
            token), cancellationToken);

    [HttpPost("actions/tenant-notification")]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<MaintenanceOperationsDetailDto>> NotifyTenant(
        Guid maintenanceRequestId,
        [FromBody] NotifyTenantCommand command,
        CancellationToken cancellationToken) =>
        RunActionAsync((createdBy, token) => maintenanceOperationsService.NotifyTenantAsync(
            maintenanceRequestId,
            command,
            createdBy,
            token), cancellationToken);

    [HttpPost("actions/internal-note")]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<MaintenanceOperationsDetailDto>> LogInternalNote(
        Guid maintenanceRequestId,
        [FromBody] LogInternalNoteCommand command,
        CancellationToken cancellationToken) =>
        RunActionAsync((createdBy, token) => maintenanceOperationsService.LogInternalNoteAsync(
            maintenanceRequestId,
            command,
            createdBy,
            token), cancellationToken);

    [HttpPost("resolution-feedback")]
    [ProducesResponseType<MaintenanceOperationsDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceOperationsDetailDto>> SubmitResolutionFeedback(
        Guid maintenanceRequestId,
        [FromBody] SubmitMaintenanceResolutionFeedbackCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var resolvedBy = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "staff";
            return Ok(await maintenanceOperationsService.SubmitResolutionFeedbackAsync(
                maintenanceRequestId,
                command,
                resolvedBy,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid resolution feedback",
                Detail = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Maintenance request not found",
                Detail = exception.Message
            });
        }
    }

    private async Task<ActionResult<MaintenanceOperationsDetailDto>> RunActionAsync(
        Func<string, CancellationToken, Task<MaintenanceOperationsDetailDto>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var createdBy = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "staff";
            return Ok(await action(createdBy, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid maintenance action",
                Detail = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Maintenance request not found",
                Detail = exception.Message
            });
        }
    }
}
