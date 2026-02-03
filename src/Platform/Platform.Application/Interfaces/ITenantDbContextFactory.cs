using Microsoft.EntityFrameworkCore;

namespace Platform.Application.Interfaces;

public interface ITenantDbContextFactory
{
    DbContext CreateDbContext(string connectionString);
}
