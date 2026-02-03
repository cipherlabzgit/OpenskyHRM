using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using TenantEntity = Platform.Domain.Entities.Tenant;

namespace Platform.Infrastructure.Data;

public class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<TenantEntity> Tenants { get; set; }
    public DbSet<TenantProvisioningJob> TenantProvisioningJobs { get; set; }
    public DbSet<TenantSchemaState> TenantSchemaStates { get; set; }
    public DbSet<BillingEventOutbox> BillingEventOutboxes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}
