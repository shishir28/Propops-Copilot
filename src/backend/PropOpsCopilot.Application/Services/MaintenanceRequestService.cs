using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Services;

public sealed class MaintenanceRequestService(IMaintenanceRequestRepository maintenanceRequestRepository)
{
    public async Task<IReadOnlyList<MaintenanceRequestDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var requests = await maintenanceRequestRepository.ListAsync(cancellationToken);
        return requests.Select(Map).ToArray();
    }

    public async Task<MaintenanceRequestDto> CreateAsync(
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = MaintenanceRequest.Create(
            command.SubmitterName,
            command.EmailAddress,
            command.PhoneNumber,
            command.PropertyName,
            command.UnitNumber,
            command.Description,
            command.Category,
            command.Priority,
            command.Channel);

        await maintenanceRequestRepository.AddAsync(request, cancellationToken);

        return Map(request);
    }

    internal static MaintenanceRequestDto Map(MaintenanceRequest request) =>
        new(
            request.Id,
            request.ReferenceNumber,
            request.SubmitterName,
            request.EmailAddress,
            request.PhoneNumber,
            request.PropertyName,
            request.UnitNumber,
            request.Description,
            request.InternalSummary,
            request.AssignedTeam,
            request.Category,
            request.Priority,
            request.Status,
            request.Channel,
            request.SubmittedAtUtc,
            request.TargetResponseByUtc);
}
