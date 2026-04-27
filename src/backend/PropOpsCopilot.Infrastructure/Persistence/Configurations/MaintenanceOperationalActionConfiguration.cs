using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceOperationalActionConfiguration : IEntityTypeConfiguration<MaintenanceOperationalAction>
{
    public void Configure(EntityTypeBuilder<MaintenanceOperationalAction> builder)
    {
        builder.ToTable("maintenance_operational_actions");

        builder.HasKey(action => action.Id);

        builder.Property(action => action.ActionType).HasConversion<string>().HasMaxLength(60);
        builder.Property(action => action.Detail).HasMaxLength(2000).IsRequired();
        builder.Property(action => action.ExternalReference).HasMaxLength(80).IsRequired();
        builder.Property(action => action.CreatedBy).HasMaxLength(256).IsRequired();

        builder.HasOne(action => action.MaintenanceRequest)
            .WithMany()
            .HasForeignKey(action => action.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(action => action.MaintenanceRequestId);
        builder.HasIndex(action => action.CreatedAtUtc);
    }
}
