using Tenant.Application.Interfaces;

namespace Tenant.Infrastructure.Services;

public class TenantConnectionStringProvider : ITenantConnectionStringProvider
{
    private string? _connectionString;

    public string? GetConnectionString() => _connectionString;

    public void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }
}
