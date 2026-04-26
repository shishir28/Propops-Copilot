using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MaintenanceRequestsController(MaintenanceRequestService maintenanceRequestService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MaintenanceRequestDto>>(StatusCodes.Status200OK)]
    [Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher}")]
    public async Task<ActionResult<IReadOnlyList<MaintenanceRequestDto>>> GetAll(CancellationToken cancellationToken)
    {
        var requests = await maintenanceRequestService.ListAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost]
    [ProducesResponseType<MaintenanceRequestDto>(StatusCodes.Status201Created)]
    [Authorize(Roles = $"{PortalRoles.PropertyManager},{PortalRoles.Dispatcher},{PortalRoles.Tenant},{PortalRoles.PropertyOwner}")]
    public async Task<ActionResult<MaintenanceRequestDto>> Create(
        [FromBody] CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = await maintenanceRequestService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = request.Id }, request);
    }
}
