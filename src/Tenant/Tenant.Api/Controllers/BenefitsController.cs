using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BenefitsController : ControllerBase
{
    private readonly TenantDbContext _context;

    public BenefitsController(TenantDbContext context)
    {
        _context = context;
    }

    // Benefit Plans
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans([FromQuery] BenefitType? type)
    {
        var query = _context.BenefitPlans.Where(p => p.IsActive).AsQueryable();
        if (type.HasValue)
            query = query.Where(p => p.Type == type.Value);

        var plans = await query
            .Select(p => new {
                p.Id, p.Name, p.Code, p.Type, p.Description, p.Provider,
                p.EmployerContribution, p.EmployeeContribution, p.ContributionType,
                EnrolledCount = p.EmployeeBenefits.Count(eb => eb.Status == EnrollmentStatus.Active)
            })
            .ToListAsync();
        return Ok(plans);
    }

    [HttpGet("plans/{id}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var plan = await _context.BenefitPlans
            .Include(p => p.EmployeeBenefits)
            .ThenInclude(eb => eb.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateBenefitPlanRequest request)
    {
        var plan = new BenefitPlan
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            Type = request.Type,
            Description = request.Description,
            Provider = request.Provider,
            EmployerContribution = request.EmployerContribution,
            EmployeeContribution = request.EmployeeContribution,
            ContributionType = request.ContributionType,
            EligibilityCriteria = request.EligibilityCriteria,
            WaitingPeriodDays = request.WaitingPeriodDays,
            EnrollmentStartDate = request.EnrollmentStartDate,
            EnrollmentEndDate = request.EnrollmentEndDate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.BenefitPlans.Add(plan);
        await _context.SaveChangesAsync();
        return Ok(plan);
    }

    // Employee Benefits
    [HttpGet("enrollments")]
    public async Task<IActionResult> GetEnrollments([FromQuery] Guid? employeeId, [FromQuery] Guid? planId)
    {
        var query = _context.EmployeeBenefits
            .Include(eb => eb.Employee)
            .Include(eb => eb.BenefitPlan)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(eb => eb.EmployeeId == employeeId.Value);
        if (planId.HasValue)
            query = query.Where(eb => eb.BenefitPlanId == planId.Value);

        var enrollments = await query
            .Select(eb => new {
                eb.Id, eb.EmployeeId, EmployeeName = eb.Employee.FullName,
                eb.BenefitPlanId, PlanName = eb.BenefitPlan.Name, PlanType = eb.BenefitPlan.Type,
                eb.EnrollmentDate, eb.EffectiveDate, eb.Status, eb.CoverageLevel
            })
            .ToListAsync();
        return Ok(enrollments);
    }

    [HttpPost("enrollments")]
    public async Task<IActionResult> Enroll([FromBody] EnrollBenefitRequest request)
    {
        var enrollment = new EmployeeBenefit
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            BenefitPlanId = request.BenefitPlanId,
            EnrollmentDate = DateTime.UtcNow,
            EffectiveDate = request.EffectiveDate,
            CoverageLevel = request.CoverageLevel,
            EmployeeContribution = request.EmployeeContribution,
            EmployerContribution = request.EmployerContribution,
            Status = EnrollmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.EmployeeBenefits.Add(enrollment);
        await _context.SaveChangesAsync();
        return Ok(enrollment);
    }

    [HttpPut("enrollments/{id}/status")]
    public async Task<IActionResult> UpdateEnrollmentStatus(Guid id, [FromBody] UpdateEnrollmentStatusRequest request)
    {
        var enrollment = await _context.EmployeeBenefits.FindAsync(id);
        if (enrollment == null) return NotFound();
        enrollment.Status = request.Status;
        if (request.Status == EnrollmentStatus.Terminated)
            enrollment.TerminationDate = DateTime.UtcNow;
        enrollment.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(enrollment);
    }
}

public class CreateBenefitPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public BenefitType Type { get; set; }
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public decimal? EmployerContribution { get; set; }
    public decimal? EmployeeContribution { get; set; }
    public string? ContributionType { get; set; }
    public string? EligibilityCriteria { get; set; }
    public int? WaitingPeriodDays { get; set; }
    public DateTime? EnrollmentStartDate { get; set; }
    public DateTime? EnrollmentEndDate { get; set; }
}

public class EnrollBenefitRequest
{
    public Guid EmployeeId { get; set; }
    public Guid BenefitPlanId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? CoverageLevel { get; set; }
    public decimal? EmployeeContribution { get; set; }
    public decimal? EmployerContribution { get; set; }
}

public class UpdateEnrollmentStatusRequest
{
    public EnrollmentStatus Status { get; set; }
}
