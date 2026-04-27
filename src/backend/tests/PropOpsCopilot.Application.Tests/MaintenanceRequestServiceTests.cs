using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Tests;

public sealed class MaintenanceRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsAndMapsTheCreatedRequest()
    {
        var repository = new CapturingMaintenanceRequestRepository();
        var service = new MaintenanceRequestService(repository);
        var command = new CreateMaintenanceRequestCommand(
            "Jordan Blake",
            "manager@propops.local",
            "0412200100",
            "Harbour View Residences",
            "22A",
            "Bathroom tap is leaking constantly.",
            MaintenanceRequestCategory.Plumbing,
            MaintenanceRequestPriority.High,
            IntakeChannel.Portal);

        var result = await service.CreateAsync(command);

        Assert.NotNull(repository.AddedRequest);
        Assert.Equal(command.SubmitterName, repository.AddedRequest.SubmitterName);
        Assert.Equal(command.PropertyName, repository.AddedRequest.PropertyName);
        Assert.Equal(command.Description, repository.AddedRequest.Description);
        Assert.Equal(command.Category, repository.AddedRequest.Category);
        Assert.Equal(command.Priority, repository.AddedRequest.Priority);
        Assert.Equal(command.Channel, repository.AddedRequest.Channel);
        Assert.Equal(repository.AddedRequest.ReferenceNumber, result.ReferenceNumber);
        Assert.Equal("Plumbing Partners", result.AssignedTeam);
        Assert.Equal(MaintenanceRequestStatus.New, result.Status);
    }

    [Fact]
    public async Task ListAsync_MapsAllRequestsReturnedByTheRepository()
    {
        var requests = new[]
        {
            MaintenanceRequest.Create(
                "Jordan Blake",
                "manager@propops.local",
                "0412200100",
                "Harbour View Residences",
                "22A",
                "Bathroom tap is leaking constantly.",
                MaintenanceRequestCategory.Plumbing,
                MaintenanceRequestPriority.High,
                IntakeChannel.Portal),
            MaintenanceRequest.Create(
                "Ava Thompson",
                "tenant@propops.local",
                "0412200101",
                "Cityscape Lofts",
                "19D",
                "Dishwasher stopped mid-cycle.",
                MaintenanceRequestCategory.Appliances,
                MaintenanceRequestPriority.Normal,
                IntakeChannel.PhoneNote)
        };

        var service = new MaintenanceRequestService(new ListingMaintenanceRequestRepository(requests));

        var result = await service.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(requests[0].ReferenceNumber, result[0].ReferenceNumber);
        Assert.Equal(requests[1].AssignedTeam, result[1].AssignedTeam);
        Assert.Equal(requests[1].Channel, result[1].Channel);
    }

    private sealed class CapturingMaintenanceRequestRepository : IMaintenanceRequestRepository
    {
        public MaintenanceRequest? AddedRequest { get; private set; }

        public Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MaintenanceRequest>>([]);

        public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<MaintenanceRequest?>(null);

        public Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
        {
            AddedRequest = request;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ListingMaintenanceRequestRepository(IReadOnlyList<MaintenanceRequest> requests) : IMaintenanceRequestRepository
    {
        public Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(requests);

        public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(requests.FirstOrDefault(request => request.Id == id));

        public Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
