using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Infrastructure.Data.Configurations;

public class TenantSchemaStateConfiguration : IEntityTypeConfiguration<TenantSchemaState>
{
    public void Configure(EntityTypeBuilder<TenantSchemaState> builder)
    {
        builder.ToTable("TenantSchemaStates");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.LastAppliedMigration)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(s => s.TenantId)
            .IsUnique();

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.SchemaStates)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
