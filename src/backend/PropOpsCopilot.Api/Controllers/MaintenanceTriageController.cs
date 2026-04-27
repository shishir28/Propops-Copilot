using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Api.Controllers;

[ApiController]
[Route("api/ai/maintenance-triage")]
[Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher}")]
public sealed class MaintenanceTriageController(MaintenanceTriagePreparationService maintenanceTriagePreparationService)
    : ControllerBase
{
    [HttpGet("contracts")]
    [ProducesResponseType<MaintenanceTriageContractsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MaintenanceTriageContractsDto>> GetContracts(CancellationToken cancellationToken)
    {
        try
        {
            var contracts = await maintenanceTriagePreparationService.GetContractsAsync(cancellationToken);
            return Ok(contracts);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "AI service unavailable",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("prepare")]
    [ProducesResponseType<MaintenanceTriagePreparationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MaintenanceTriagePreparationResultDto>> Prepare(
        [FromBody] PrepareMaintenanceTriageCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var prepared = await maintenanceTriagePreparationService.PrepareAsync(command, cancellationToken);
            return Ok(prepared);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid triage request",
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
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "AI service unavailable",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("infer")]
    [ProducesResponseType<MaintenanceTriageInferenceResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MaintenanceTriageInferenceResultDto>> Infer(
        [FromBody] PrepareMaintenanceTriageCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var inferred = await maintenanceTriagePreparationService.InferAsync(command, cancellationToken);
            return Ok(inferred);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid triage request",
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
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "AI service unavailable",
                Detail = exception.Message
            });
        }
    }
}
