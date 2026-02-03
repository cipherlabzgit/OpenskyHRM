using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Infrastructure.Data.Configurations;

public class TenantProvisioningJobConfiguration : IEntityTypeConfiguration<TenantProvisioningJob>
{
    public void Configure(EntityTypeBuilder<TenantProvisioningJob> builder)
    {
        builder.ToTable("TenantProvisioningJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.LastError)
            .HasMaxLength(2000);

        builder.HasOne(j => j.Tenant)
            .WithMany(t => t.ProvisioningJobs)
            .HasForeignKey(j => j.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
