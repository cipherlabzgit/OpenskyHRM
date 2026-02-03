using Microsoft.EntityFrameworkCore;
using Platform.Application.Interfaces;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using TenantEntity = Platform.Domain.Entities.Tenant;

namespace Platform.Infrastructure.Repositories;

public class TenantsRepository : ITenantsRepository
{
    private readonly PlatformDbContext _context;

    public TenantsRepository(PlatformDbContext context)
    {
        _context = context;
    }

    public async Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
    }

    public async Task<TenantEntity?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantCode == tenantCode, cancellationToken);
    }

    public async Task<TenantEntity?> GetByAdminEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.AdminEmail == normalizedEmail && t.Status == TenantStatus.Active, cancellationToken);
    }

    public async Task<IEnumerable<TenantEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants.ToListAsync(cancellationToken);
    }

    public async Task<TenantEntity> CreateAsync(TenantEntity tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    public async Task UpdateAsync(TenantEntity tenant, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(tenantId, cancellationToken);
        if (tenant != null)
        {
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<TenantProvisioningJob> CreateProvisioningJobAsync(TenantProvisioningJob job, CancellationToken cancellationToken = default)
    {
        _context.TenantProvisioningJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task UpdateProvisioningJobAsync(TenantProvisioningJob job, CancellationToken cancellationToken = default)
    {
        _context.TenantProvisioningJobs.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
