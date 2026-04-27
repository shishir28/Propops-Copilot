using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Infrastructure.Persistence;

public sealed class PropOpsDbContext(DbContextOptions<PropOpsDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<ContactDirectoryEntry> ContactDirectoryEntries => Set<ContactDirectoryEntry>();

    public DbSet<IntakeSubmission> IntakeSubmissions => Set<IntakeSubmission>();

    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();

    public DbSet<MaintenanceTriageReview> MaintenanceTriageReviews => Set<MaintenanceTriageReview>();

    public DbSet<MaintenanceOperationalAction> MaintenanceOperationalActions => Set<MaintenanceOperationalAction>();

    public DbSet<MaintenanceResolutionFeedback> MaintenanceResolutionFeedback => Set<MaintenanceResolutionFeedback>();

    public DbSet<FineTuningExampleCandidate> FineTuningExampleCandidates => Set<FineTuningExampleCandidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropOpsDbContext).Assembly);
    }
}
