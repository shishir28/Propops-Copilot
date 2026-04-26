using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher}")]
[Route("api/[controller]")]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType<DashboardOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await dashboardService.GetOverviewAsync(cancellationToken);
        return Ok(overview);
    }
}
