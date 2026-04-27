using Microsoft.Extensions.Options;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;
using PropOpsCopilot.Application.Options;
using PropOpsCopilot.Application.Services;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;

namespace PropOpsCopilot.Application.Tests;

public sealed class OmnichannelIntakeServiceTests
{
    [Fact]
    public async Task IngestEmailAsync_NormalizesPayloadAndCreatesQueueReadyEntities()
    {
        var submissionRepository = new CapturingIntakeSubmissionRepository();
        var contactDirectoryRepository = new StubContactDirectoryRepository(
            ContactDirectoryEntry.Create(
                "Ava Thompson",
                "ava.thompson@example.com",
                "0412 200 100",
                "Harbour View Residences",
                "12B",
                "Ava Thompson"));
        var service = CreateService(submissionRepository, contactDirectoryRepository);
        var command = new IngestEmailIntakeCommand(
            " Ava Thompson ",
            "AVA.THOMPSON@EXAMPLE.COM ",
            "  Leaking sink in 12B  ",
            "Kitchen sink is leaking heavily under the cabinet.\n\nWater is spreading across the floor.",
            "0412 200 100",
            string.Empty,
            string.Empty,
            " EMAIL-123 ",
            new DateTimeOffset(2026, 4, 25, 20, 30, 0, TimeSpan.Zero));

        var result = await service.IngestEmailAsync(command);

        Assert.NotNull(submissionRepository.SavedSubmission);
        Assert.NotNull(submissionRepository.SavedMaintenanceRequest);
        Assert.Equal(IntakeChannel.Email, result.Submission.StandardizedPayload.Channel);
        Assert.Equal("EMAIL-123", result.Submission.StandardizedPayload.SourceReference);
        Assert.Equal("Harbour View Residences", result.Submission.StandardizedPayload.PropertyName);
        Assert.Equal("12B", result.Submission.StandardizedPayload.UnitNumber);
        Assert.Equal(MaintenanceRequestCategory.Plumbing, result.Submission.StandardizedPayload.Category);
        Assert.Equal(MaintenanceRequestPriority.High, result.Submission.StandardizedPayload.Priority);
        Assert.True(result.Submission.StandardizedPayload.IsAfterHours);
        Assert.True(result.Submission.StandardizedPayload.MetadataMatched);
        Assert.Contains("Leaking sink in 12B", result.Submission.StandardizedPayload.NormalizedContent);
        Assert.Contains("Kitchen sink is leaking heavily under the cabinet.", result.Submission.StandardizedPayload.NormalizedContent);
        Assert.Equal(result.MaintenanceRequest.ReferenceNumber, submissionRepository.SavedMaintenanceRequest.ReferenceNumber);
        Assert.Equal(IntakeChannel.Email, submissionRepository.SavedMaintenanceRequest.Channel);
    }

    [Fact]
    public async Task IngestSmsChatAsync_ThrowsWhenPropertyMetadataCannotBeResolved()
    {
        var service = CreateService(
            new CapturingIntakeSubmissionRepository(),
            new StubContactDirectoryRepository(null));
        var command = new IngestSmsChatIntakeCommand(
            "Unknown Person",
            "0412200102",
            "Something is broken and I need help.",
            string.Empty,
            string.Empty,
            string.Empty,
            "SMS-123",
            new DateTimeOffset(2026, 4, 25, 11, 30, 0, TimeSpan.Zero));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.IngestSmsChatAsync(command));

        Assert.Contains("Unable to resolve property metadata", exception.Message);
    }

    private static OmnichannelIntakeService CreateService(
        IIntakeSubmissionRepository submissionRepository,
        IContactDirectoryRepository contactDirectoryRepository)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IntakePreprocessingOptions
        {
            TimeZoneId = "UTC",
            BusinessHoursStartHour = 8,
            BusinessHoursEndHour = 18
        });

        return new OmnichannelIntakeService(submissionRepository, contactDirectoryRepository, options);
    }

    private sealed class CapturingIntakeSubmissionRepository : IIntakeSubmissionRepository
    {
        public IntakeSubmission? SavedSubmission { get; private set; }

        public MaintenanceRequest? SavedMaintenanceRequest { get; private set; }

        public Task AddWithRequestAsync(
            IntakeSubmission submission,
            MaintenanceRequest maintenanceRequest,
            CancellationToken cancellationToken = default)
        {
            SavedSubmission = submission;
            SavedMaintenanceRequest = maintenanceRequest;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IntakeSubmission>> ListRecentAsync(int take = 12, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IntakeSubmission>>([]);
    }

    private sealed class StubContactDirectoryRepository(ContactDirectoryEntry? directoryEntry) : IContactDirectoryRepository
    {
        public Task<ContactDirectoryEntry?> FindByEmailAsync(string emailAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult(directoryEntry?.EmailAddress == emailAddress ? directoryEntry : null);

        public Task<ContactDirectoryEntry?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(directoryEntry?.PhoneNumber == phoneNumber ? directoryEntry : null);
    }
}
