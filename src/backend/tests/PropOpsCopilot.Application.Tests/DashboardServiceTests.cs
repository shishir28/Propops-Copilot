using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_ComputesQueueMetricsFromRepositoryData()
    {
        var plumbing = MaintenanceRequest.Create(
            "Jordan Blake",
            "manager@propops.local",
            "0412200100",
            "Harbour View Residences",
            "22A",
            "Kitchen sink leak",
            MaintenanceRequestCategory.Plumbing,
            MaintenanceRequestPriority.High,
            IntakeChannel.Portal);
        var security = MaintenanceRequest.Create(
            "Casey Morgan",
            "dispatcher@propops.local",
            "0412200101",
            "Elm Street Townhomes",
            "3",
            "Front door lock jammed",
            MaintenanceRequestCategory.Security,
            MaintenanceRequestPriority.Emergency,
            IntakeChannel.PhoneNote);
        var general = MaintenanceRequest.Create(
            "Ava Thompson",
            "tenant@propops.local",
            "0412200102",
            "Cityscape Lofts",
            "19D",
            "Minor cosmetic patch required",
            MaintenanceRequestCategory.General,
            MaintenanceRequestPriority.Low,
            IntakeChannel.Email);
        general.TransitionTo(MaintenanceRequestStatus.Completed);

        var service = new DashboardService(new StubMaintenanceRequestRepository([plumbing, security, general]));

        var overview = await service.GetOverviewAsync();

        Assert.Equal(2, overview.OpenRequests);
        Assert.Equal(2, overview.UrgentRequests);
        Assert.Equal(3, overview.TodaySubmissions);
        Assert.Equal(9.7m, overview.AverageResponseHours);
        Assert.Equal(3, overview.RecentRequests.Count);
        Assert.Equal(plumbing.ReferenceNumber, overview.RecentRequests[0].ReferenceNumber);
        Assert.Equal(security.ReferenceNumber, overview.RecentRequests[1].ReferenceNumber);
        Assert.Equal(general.ReferenceNumber, overview.RecentRequests[2].ReferenceNumber);
    }

    private sealed class StubMaintenanceRequestRepository(IReadOnlyList<MaintenanceRequest> requests) : IMaintenanceRequestRepository
    {
        public Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(requests);

        public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(requests.FirstOrDefault(request => request.Id == id));

        public Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
