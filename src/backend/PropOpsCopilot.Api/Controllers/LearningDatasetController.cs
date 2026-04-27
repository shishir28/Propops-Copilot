using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Api.Controllers;

[ApiController]
[Route("api/learning/dataset")]
[Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher}")]
public sealed class LearningDatasetController(LearningDatasetService learningDatasetService) : ControllerBase
{
    [HttpGet("candidates")]
    [ProducesResponseType<IReadOnlyList<FineTuningExampleCandidateDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FineTuningExampleCandidateDto>>> GetCandidates(
        CancellationToken cancellationToken)
    {
        var candidates = await learningDatasetService.ListCandidatesAsync(cancellationToken);
        return Ok(candidates);
    }

    [HttpGet("export")]
    [ProducesResponseType<FineTuningDatasetExportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FineTuningDatasetExportDto>> Export(CancellationToken cancellationToken)
    {
        var export = await learningDatasetService.ExportJsonlAsync(cancellationToken);
        return Ok(export);
    }
}
