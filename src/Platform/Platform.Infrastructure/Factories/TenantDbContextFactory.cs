using Microsoft.EntityFrameworkCore;
using Platform.Application.Interfaces;

namespace Platform.Infrastructure.Factories;

// Simple implementation that creates a generic DbContext
// The actual TenantDbContext is not needed here since we're just using raw SQL in provisioning
public class TenantDbContextFactory : ITenantDbContextFactory
{
    public DbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SimpleDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new SimpleDbContext(optionsBuilder.Options);
    }
}

// Simple DbContext for basic database operations
public class SimpleDbContext : DbContext
{
    public SimpleDbContext(DbContextOptions<SimpleDbContext> options) : base(options)
    {
    }
}
