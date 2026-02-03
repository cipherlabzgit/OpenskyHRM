namespace Tenant.Application.Interfaces;

public interface ITenantConnectionStringProvider
{
    string? GetConnectionString();
    void SetConnectionString(string connectionString);
}
