using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class IntakeSubmissionConfiguration : IEntityTypeConfiguration<IntakeSubmission>
{
    public void Configure(EntityTypeBuilder<IntakeSubmission> builder)
    {
        builder.ToTable("intake_submissions");

        builder.HasKey(submission => submission.Id);

        builder.Property(submission => submission.SourceReference).HasMaxLength(80).IsRequired();
        builder.Property(submission => submission.Channel).HasConversion<string>().HasMaxLength(40);
        builder.Property(submission => submission.SubmitterName).HasMaxLength(120).IsRequired();
        builder.Property(submission => submission.TenantName).HasMaxLength(120).IsRequired();
        builder.Property(submission => submission.EmailAddress).HasMaxLength(256);
        builder.Property(submission => submission.PhoneNumber).HasMaxLength(32);
        builder.Property(submission => submission.PropertyName).HasMaxLength(160).IsRequired();
        builder.Property(submission => submission.UnitNumber).HasMaxLength(40);
        builder.Property(submission => submission.Subject).HasMaxLength(240);
        builder.Property(submission => submission.RawContent).HasMaxLength(6000).IsRequired();
        builder.Property(submission => submission.NormalizedContent).HasMaxLength(4000).IsRequired();
        builder.Property(submission => submission.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(submission => submission.Priority).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(submission => submission.SourceReference);
        builder.HasIndex(submission => submission.ReceivedAtUtc);

        builder.HasOne(submission => submission.MaintenanceRequest)
            .WithMany()
            .HasForeignKey(submission => submission.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
