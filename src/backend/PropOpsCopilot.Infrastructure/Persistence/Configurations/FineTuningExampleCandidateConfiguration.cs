using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class FineTuningExampleCandidateConfiguration : IEntityTypeConfiguration<FineTuningExampleCandidate>
{
    public void Configure(EntityTypeBuilder<FineTuningExampleCandidate> builder)
    {
        builder.ToTable("fine_tuning_example_candidates");

        builder.HasKey(candidate => candidate.Id);

        builder.Property(candidate => candidate.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(candidate => candidate.InputSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(candidate => candidate.OutputSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(candidate => candidate.MetadataSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(candidate => candidate.ExclusionReason).HasMaxLength(1000).IsRequired();

        builder.HasOne(candidate => candidate.MaintenanceResolutionFeedback)
            .WithMany()
            .HasForeignKey(candidate => candidate.MaintenanceResolutionFeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(candidate => candidate.MaintenanceRequestId);
        builder.HasIndex(candidate => candidate.CreatedAtUtc);
        builder.HasIndex(candidate => candidate.Status);
    }
}
