using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TrainingController : ControllerBase
{
    private readonly TenantDbContext _context;

    public TrainingController(TenantDbContext context)
    {
        _context = context;
    }

    // Training Catalog
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] string? category, [FromQuery] TrainingType? type)
    {
        var query = _context.Trainings.Where(t => t.IsActive).AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);
        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        var trainings = await query
            .Select(t => new {
                t.Id, t.Title, t.Code, t.Description, t.Type, t.Category,
                t.Provider, t.DurationHours, t.Cost, t.IsMandatory, t.HasCertification,
                EnrolledCount = t.EmployeeTrainings.Count
            })
            .ToListAsync();
        return Ok(trainings);
    }

    [HttpGet("catalog/{id}")]
    public async Task<IActionResult> GetTraining(Guid id)
    {
        var training = await _context.Trainings
            .Include(t => t.EmployeeTrainings)
            .ThenInclude(et => et.Employee)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (training == null) return NotFound();
        return Ok(training);
    }

    [HttpPost("catalog")]
    public async Task<IActionResult> CreateTraining([FromBody] CreateTrainingRequest request)
    {
        var training = new Training
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Code = request.Code,
            Description = request.Description,
            Type = request.Type,
            Category = request.Category,
            Provider = request.Provider,
            Instructor = request.Instructor,
            DurationHours = request.DurationHours,
            Cost = request.Cost,
            Currency = request.Currency,
            Location = request.Location,
            OnlineUrl = request.OnlineUrl,
            MaxParticipants = request.MaxParticipants,
            IsMandatory = request.IsMandatory,
            HasCertification = request.HasCertification,
            ValidityMonths = request.ValidityMonths,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Trainings.Add(training);
        await _context.SaveChangesAsync();
        return Ok(training);
    }

    // Employee Training Assignments
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments([FromQuery] Guid? employeeId, [FromQuery] TrainingStatus? status)
    {
        var query = _context.EmployeeTrainings
            .Include(et => et.Employee)
            .Include(et => et.Training)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(et => et.EmployeeId == employeeId.Value);
        if (status.HasValue)
            query = query.Where(et => et.Status == status.Value);

        var assignments = await query
            .OrderByDescending(et => et.AssignedDate)
            .Select(et => new {
                et.Id, et.EmployeeId, EmployeeName = et.Employee.FullName,
                et.TrainingId, TrainingTitle = et.Training.Title,
                et.AssignedDate, et.DueDate, et.StartDate, et.CompletedDate,
                et.Status, et.ProgressPercent, et.Score, et.Passed
            })
            .ToListAsync();
        return Ok(assignments);
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> AssignTraining([FromBody] AssignTrainingRequest request)
    {
        var assignment = new EmployeeTraining
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            TrainingId = request.TrainingId,
            AssignedDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            Status = TrainingStatus.Assigned,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.EmployeeTrainings.Add(assignment);
        await _context.SaveChangesAsync();
        return Ok(assignment);
    }

    [HttpPut("assignments/{id}")]
    public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] UpdateTrainingAssignmentRequest request)
    {
        var assignment = await _context.EmployeeTrainings.FindAsync(id);
        if (assignment == null) return NotFound();

        assignment.Status = request.Status;
        assignment.ProgressPercent = request.ProgressPercent;
        assignment.Score = request.Score;
        assignment.Passed = request.Passed;
        assignment.Feedback = request.Feedback;
        assignment.Rating = request.Rating;
        
        if (request.Status == TrainingStatus.InProgress && assignment.StartDate == null)
            assignment.StartDate = DateTime.UtcNow;
        if (request.Status == TrainingStatus.Completed)
            assignment.CompletedDate = DateTime.UtcNow;
            
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(assignment);
    }

    // Certifications
    [HttpGet("certifications")]
    public async Task<IActionResult> GetCertifications([FromQuery] Guid? employeeId)
    {
        var query = _context.EmployeeTrainings
            .Include(et => et.Employee)
            .Include(et => et.Training)
            .Where(et => et.Training.HasCertification && et.Status == TrainingStatus.Completed && et.Passed == true);

        if (employeeId.HasValue)
            query = query.Where(et => et.EmployeeId == employeeId.Value);

        var certifications = await query
            .Select(et => new {
                et.Id, et.EmployeeId, EmployeeName = et.Employee.FullName,
                et.TrainingId, TrainingTitle = et.Training.Title,
                et.CertificateNumber, et.CompletedDate, et.CertificateExpiryDate,
                IsExpired = et.CertificateExpiryDate != null && et.CertificateExpiryDate < DateTime.UtcNow
            })
            .ToListAsync();
        return Ok(certifications);
    }
}

public class CreateTrainingRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public TrainingType Type { get; set; }
    public string? Category { get; set; }
    public string? Provider { get; set; }
    public string? Instructor { get; set; }
    public int? DurationHours { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Location { get; set; }
    public string? OnlineUrl { get; set; }
    public int? MaxParticipants { get; set; }
    public bool IsMandatory { get; set; }
    public bool HasCertification { get; set; }
    public int? ValidityMonths { get; set; }
}

public class AssignTrainingRequest
{
    public Guid EmployeeId { get; set; }
    public Guid TrainingId { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTrainingAssignmentRequest
{
    public TrainingStatus Status { get; set; }
    public int? ProgressPercent { get; set; }
    public decimal? Score { get; set; }
    public bool? Passed { get; set; }
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
}
