using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.ReferenceNumber).HasMaxLength(32).IsRequired();
        builder.Property(request => request.SubmitterName).HasMaxLength(120).IsRequired();
        builder.Property(request => request.EmailAddress).HasMaxLength(256);
        builder.Property(request => request.PhoneNumber).HasMaxLength(40);
        builder.Property(request => request.PropertyName).HasMaxLength(160).IsRequired();
        builder.Property(request => request.UnitNumber).HasMaxLength(40);
        builder.Property(request => request.Description).HasMaxLength(4000).IsRequired();
        builder.Property(request => request.InternalSummary).HasMaxLength(512).IsRequired();
        builder.Property(request => request.AssignedTeam).HasMaxLength(120).IsRequired();
        builder.Property(request => request.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(request => request.Priority).HasConversion<string>().HasMaxLength(40);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(request => request.Channel).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(request => request.ReferenceNumber).IsUnique();
        builder.HasIndex(request => request.SubmittedAtUtc);
    }
}
