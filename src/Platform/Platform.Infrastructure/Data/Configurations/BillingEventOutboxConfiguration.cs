using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Infrastructure.Data.Configurations;

public class BillingEventOutboxConfiguration : IEntityTypeConfiguration<BillingEventOutbox>
{
    public void Configure(EntityTypeBuilder<BillingEventOutbox> builder)
    {
        builder.ToTable("BillingEventOutboxes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Processed)
            .HasDefaultValue(false);

        builder.HasIndex(e => new { e.TenantId, e.Processed, e.CreatedAtUtc });

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.BillingEvents)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
