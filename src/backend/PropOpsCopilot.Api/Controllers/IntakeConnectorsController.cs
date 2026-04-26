using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Api.Controllers;

[ApiController]
[Route("api/intakeConnectors")]
[Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher}")]
public sealed class IntakeConnectorsController(OmnichannelIntakeService omnichannelIntakeService) : ControllerBase
{
    [HttpGet("recent")]
    [ProducesResponseType<IReadOnlyList<IntakeSubmissionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IntakeSubmissionDto>>> GetRecent(CancellationToken cancellationToken)
    {
        var submissions = await omnichannelIntakeService.ListRecentAsync(cancellationToken);
        return Ok(submissions);
    }

    [HttpPost("email")]
    [ProducesResponseType<IntakeIngestionResultDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IntakeIngestionResultDto>> IngestEmail(
        [FromBody] IngestEmailIntakeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await omnichannelIntakeService.IngestEmailAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetRecent), new { id = result.Submission.Id }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid intake payload", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Intake preprocessing failed", Detail = exception.Message });
        }
    }

    [HttpPost("sms-chat")]
    [ProducesResponseType<IntakeIngestionResultDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IntakeIngestionResultDto>> IngestSmsChat(
        [FromBody] IngestSmsChatIntakeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await omnichannelIntakeService.IngestSmsChatAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetRecent), new { id = result.Submission.Id }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid intake payload", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Intake preprocessing failed", Detail = exception.Message });
        }
    }

    [HttpPost("phone-note")]
    [ProducesResponseType<IntakeIngestionResultDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IntakeIngestionResultDto>> IngestPhoneNote(
        [FromBody] IngestPhoneNoteIntakeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await omnichannelIntakeService.IngestPhoneNoteAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetRecent), new { id = result.Submission.Id }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid intake payload", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Intake preprocessing failed", Detail = exception.Message });
        }
    }
}
