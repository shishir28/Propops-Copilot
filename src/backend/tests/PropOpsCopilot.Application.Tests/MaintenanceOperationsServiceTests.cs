using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Tests;

public sealed class MaintenanceOperationsServiceTests
{
    [Fact]
    public async Task SubmitTriageReviewAsync_PersistsEditedReviewAndUpdatesRequest()
    {
        var request = CreateRequest();
        var requestRepository = new InMemoryMaintenanceRequestRepository([request]);
        var reviewRepository = new InMemoryTriageReviewRepository();
        var actionRepository = new InMemoryOperationalActionRepository();
        var service = new MaintenanceOperationsService(requestRepository, reviewRepository, actionRepository);

        var result = await service.SubmitTriageReviewAsync(
            request.Id,
            new SubmitMaintenanceTriageReviewCommand(
                new MaintenanceTriageOutputContractDto(
                    "General",
                    "Normal",
                    "General Maintenance Contractor",
                    "Route for staff review.",
                    "Normal General request.",
                    "We are reviewing the request."),
                new MaintenanceTriageGuardrailResultDto(
                    true,
                    true,
                    true,
                    0.72,
                    0.68,
                    false,
                    false,
                    []),
                MaintenanceRequestCategory.Plumbing,
                MaintenanceRequestPriority.High,
                "Licensed Plumber",
                "Create an urgent work order and assign the preferred plumber.",
                "High Plumbing issue at Harbour View Residences / 22A.",
                "Thanks, we are assigning a plumber now."),
            "manager@propops.local");

        Assert.NotNull(result.LatestReview);
        Assert.Equal(MaintenanceTriageReviewStatus.Edited, result.LatestReview.Status);
        Assert.Equal(MaintenanceRequestCategory.Plumbing, result.Request.Category);
        Assert.Equal(MaintenanceRequestPriority.High, result.Request.Priority);
        Assert.Equal(MaintenanceRequestStatus.InReview, result.Request.Status);
        Assert.Equal("Plumbing Partners", result.Request.AssignedTeam);
        Assert.Equal(request.Id, requestRepository.UpdatedRequest?.Id);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_LogsActionAndSchedulesRequest()
    {
        var request = CreateRequest();
        var service = new MaintenanceOperationsService(
            new InMemoryMaintenanceRequestRepository([request]),
            new InMemoryTriageReviewRepository(),
            new InMemoryOperationalActionRepository());

        var result = await service.CreateWorkOrderAsync(
            request.Id,
            new CreateWorkOrderCommand("Create urgent plumbing work order."),
            "dispatcher@propops.local");

        Assert.Equal(MaintenanceRequestStatus.Scheduled, result.Request.Status);
        var action = Assert.Single(result.Actions);
        Assert.Equal(MaintenanceOperationalActionType.WorkOrderCreated, action.ActionType);
        Assert.StartsWith("WO-", action.ExternalReference);
    }

    [Fact]
    public async Task NotifyTenantAsync_LogsTenantMessageWithoutChangingStatus()
    {
        var request = CreateRequest();
        request.TransitionTo(MaintenanceRequestStatus.InReview);
        var service = new MaintenanceOperationsService(
            new InMemoryMaintenanceRequestRepository([request]),
            new InMemoryTriageReviewRepository(),
            new InMemoryOperationalActionRepository());

        var result = await service.NotifyTenantAsync(
            request.Id,
            new NotifyTenantCommand("A plumber has been assigned."),
            "dispatcher@propops.local");

        Assert.Equal(MaintenanceRequestStatus.InReview, result.Request.Status);
        var action = Assert.Single(result.Actions);
        Assert.Equal(MaintenanceOperationalActionType.TenantNotified, action.ActionType);
        Assert.Equal("A plumber has been assigned.", action.Detail);
    }

    private static MaintenanceRequest CreateRequest() =>
        MaintenanceRequest.Create(
            "Jordan Blake",
            "manager@propops.local",
            "0412200100",
            "Harbour View Residences",
            "22A",
            "Kitchen sink pipe is leaking heavily under the cabinet.",
            MaintenanceRequestCategory.General,
            MaintenanceRequestPriority.Normal,
            IntakeChannel.Portal);

    private sealed class InMemoryMaintenanceRequestRepository(IReadOnlyList<MaintenanceRequest> seedRequests)
        : IMaintenanceRequestRepository
    {
        private readonly List<MaintenanceRequest> requests = [.. seedRequests];

        public MaintenanceRequest? UpdatedRequest { get; private set; }

        public Task<IReadOnlyList<MaintenanceRequest>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MaintenanceRequest>>(requests);

        public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(requests.FirstOrDefault(request => request.Id == id));

        public Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
        {
            requests.Add(request);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
        {
            UpdatedRequest = request;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTriageReviewRepository : IMaintenanceTriageReviewRepository
    {
        private readonly List<MaintenanceTriageReview> reviews = [];

        public Task<MaintenanceTriageReview?> GetLatestForRequestAsync(
            Guid maintenanceRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(reviews
                .Where(review => review.MaintenanceRequestId == maintenanceRequestId)
                .OrderByDescending(review => review.ReviewedAtUtc)
                .FirstOrDefault());

        public Task AddAsync(MaintenanceTriageReview review, CancellationToken cancellationToken = default)
        {
            reviews.Add(review);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOperationalActionRepository : IMaintenanceOperationalActionRepository
    {
        private readonly List<MaintenanceOperationalAction> actions = [];

        public Task<IReadOnlyList<MaintenanceOperationalAction>> ListForRequestAsync(
            Guid maintenanceRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MaintenanceOperationalAction>>(
                actions
                    .Where(action => action.MaintenanceRequestId == maintenanceRequestId)
                    .OrderByDescending(action => action.CreatedAtUtc)
                    .ToArray());

        public Task AddAsync(MaintenanceOperationalAction action, CancellationToken cancellationToken = default)
        {
            actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
