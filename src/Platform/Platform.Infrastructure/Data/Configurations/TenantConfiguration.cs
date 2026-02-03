using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;
using TenantEntity = Platform.Domain.Entities.Tenant;

namespace Platform.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.TenantId);

        builder.Property(t => t.TenantCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.TenantCode)
            .IsUnique();

        builder.Property(t => t.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.LegalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Country)
            .HasMaxLength(100);

        builder.Property(t => t.TimeZone)
            .HasMaxLength(100);

        builder.Property(t => t.Currency)
            .HasMaxLength(10);

        builder.Property(t => t.Plan)
            .HasMaxLength(50);

        builder.Property(t => t.DbName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.DbHost)
            .HasMaxLength(255);
    }
}
