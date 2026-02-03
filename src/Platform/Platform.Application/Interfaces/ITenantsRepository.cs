using TenantEntity = Platform.Domain.Entities.Tenant;
using Platform.Domain.Entities;

namespace Platform.Application.Interfaces;

public interface ITenantsRepository
{
    Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantEntity?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);
    Task<TenantEntity?> GetByAdminEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantEntity> CreateAsync(TenantEntity tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantEntity tenant, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantProvisioningJob> CreateProvisioningJobAsync(TenantProvisioningJob job, CancellationToken cancellationToken = default);
    Task UpdateProvisioningJobAsync(TenantProvisioningJob job, CancellationToken cancellationToken = default);
}
