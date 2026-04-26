using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Infrastructure.Persistence.Configurations;

public sealed class ContactDirectoryEntryConfiguration : IEntityTypeConfiguration<ContactDirectoryEntry>
{
    public void Configure(EntityTypeBuilder<ContactDirectoryEntry> builder)
    {
        builder.ToTable("contact_directory_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.FullName).HasMaxLength(120).IsRequired();
        builder.Property(entry => entry.EmailAddress).HasMaxLength(256);
        builder.Property(entry => entry.PhoneNumber).HasMaxLength(32);
        builder.Property(entry => entry.PropertyName).HasMaxLength(160).IsRequired();
        builder.Property(entry => entry.UnitNumber).HasMaxLength(40);
        builder.Property(entry => entry.TenantName).HasMaxLength(120).IsRequired();

        builder.HasIndex(entry => entry.EmailAddress);
        builder.HasIndex(entry => entry.PhoneNumber);
    }
}
