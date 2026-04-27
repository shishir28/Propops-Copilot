using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceResolutionFeedbackConfiguration : IEntityTypeConfiguration<MaintenanceResolutionFeedback>
{
    public void Configure(EntityTypeBuilder<MaintenanceResolutionFeedback> builder)
    {
        builder.ToTable("maintenance_resolution_feedback");

        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.FinalResolution).HasMaxLength(2000).IsRequired();
        builder.Property(feedback => feedback.CorrectedCategory).HasConversion<string>().HasMaxLength(40);
        builder.Property(feedback => feedback.CorrectedPriority).HasConversion<string>().HasMaxLength(40);
        builder.Property(feedback => feedback.FinalTenantResponse).HasMaxLength(1200).IsRequired();
        builder.Property(feedback => feedback.DispatchOutcome).HasConversion<string>().HasMaxLength(60);
        builder.Property(feedback => feedback.ResolutionNotes).HasMaxLength(2000).IsRequired();
        builder.Property(feedback => feedback.ExclusionReason).HasMaxLength(1000).IsRequired();
        builder.Property(feedback => feedback.ResolvedBy).HasMaxLength(256).IsRequired();

        builder.HasOne(feedback => feedback.MaintenanceRequest)
            .WithMany()
            .HasForeignKey(feedback => feedback.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(feedback => feedback.MaintenanceTriageReview)
            .WithMany()
            .HasForeignKey(feedback => feedback.MaintenanceTriageReviewId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(feedback => feedback.MaintenanceRequestId);
        builder.HasIndex(feedback => feedback.ResolvedAtUtc);
    }
}
