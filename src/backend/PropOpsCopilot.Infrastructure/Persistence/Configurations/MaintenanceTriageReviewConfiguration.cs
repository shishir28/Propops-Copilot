using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceTriageReviewConfiguration : IEntityTypeConfiguration<MaintenanceTriageReview>
{
    public void Configure(EntityTypeBuilder<MaintenanceTriageReview> builder)
    {
        builder.ToTable("maintenance_triage_reviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.AiCategory).HasConversion<string>().HasMaxLength(40);
        builder.Property(review => review.AiPriority).HasConversion<string>().HasMaxLength(40);
        builder.Property(review => review.AiVendorType).HasMaxLength(160).IsRequired();
        builder.Property(review => review.AiDispatchDecision).HasMaxLength(1000).IsRequired();
        builder.Property(review => review.AiInternalSummary).HasMaxLength(1200).IsRequired();
        builder.Property(review => review.AiTenantResponseDraft).HasMaxLength(1200).IsRequired();
        builder.Property(review => review.FinalCategory).HasConversion<string>().HasMaxLength(40);
        builder.Property(review => review.FinalPriority).HasConversion<string>().HasMaxLength(40);
        builder.Property(review => review.FinalVendorType).HasMaxLength(160).IsRequired();
        builder.Property(review => review.FinalDispatchDecision).HasMaxLength(1000).IsRequired();
        builder.Property(review => review.FinalInternalSummary).HasMaxLength(1200).IsRequired();
        builder.Property(review => review.FinalTenantResponseDraft).HasMaxLength(1200).IsRequired();
        builder.Property(review => review.GuardrailSummary).HasMaxLength(2000).IsRequired();
        builder.Property(review => review.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(review => review.ReviewedBy).HasMaxLength(256).IsRequired();

        builder.HasOne(review => review.MaintenanceRequest)
            .WithMany()
            .HasForeignKey(review => review.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(review => review.MaintenanceRequestId);
        builder.HasIndex(review => review.ReviewedAtUtc);
    }
}
