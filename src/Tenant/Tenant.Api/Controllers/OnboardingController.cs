using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly TenantDbContext _context;

    public OnboardingController(TenantDbContext context)
    {
        _context = context;
    }

    // Templates
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _context.OnboardingTemplates
            .Where(t => t.IsActive)
            .Include(t => t.Department)
            .Include(t => t.Designation)
            .Select(t => new {
                t.Id, t.Name, t.Description, t.IsDefault,
                DepartmentName = t.Department != null ? t.Department.Name : null,
                DesignationName = t.Designation != null ? t.Designation.Name : null,
                TaskCount = t.Tasks.Count
            })
            .ToListAsync();
        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplate(Guid id)
    {
        var template = await _context.OnboardingTemplates
            .Include(t => t.Tasks.OrderBy(task => task.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var template = new OnboardingTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.OnboardingTemplates.Add(template);
        await _context.SaveChangesAsync();
        return Ok(template);
    }

    [HttpPost("templates/{id}/tasks")]
    public async Task<IActionResult> AddTemplateTask(Guid id, [FromBody] CreateTemplateTaskRequest request)
    {
        var template = await _context.OnboardingTemplates.FindAsync(id);
        if (template == null) return NotFound();

        var task = new OnboardingTemplateTask
        {
            Id = Guid.NewGuid(),
            OnboardingTemplateId = id,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            SortOrder = request.SortOrder,
            DueDaysFromStart = request.DueDaysFromStart,
            AssigneeRole = request.AssigneeRole,
            IsRequired = request.IsRequired,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.OnboardingTemplateTasks.Add(task);
        await _context.SaveChangesAsync();
        return Ok(task);
    }

    // Employee Onboarding Tasks
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks([FromQuery] Guid? employeeId, [FromQuery] OnboardingTaskStatus? status)
    {
        var query = _context.OnboardingTasks
            .Include(t => t.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(t => t.EmployeeId == employeeId.Value);
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var tasks = await query
            .OrderBy(t => t.SortOrder)
            .Select(t => new {
                t.Id, t.EmployeeId, EmployeeName = t.Employee.FullName,
                t.Title, t.Description, t.Category, t.DueDate,
                t.Status, t.IsRequired, t.CompletedDate
            })
            .ToListAsync();
        return Ok(tasks);
    }

    [HttpPost("initiate/{employeeId}")]
    public async Task<IActionResult> InitiateOnboarding(Guid employeeId, [FromQuery] Guid? templateId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound("Employee not found");

        // Find template - either specified, matching department/designation, or default
        OnboardingTemplate? template = null;
        if (templateId.HasValue)
            template = await _context.OnboardingTemplates.Include(t => t.Tasks).FirstOrDefaultAsync(t => t.Id == templateId.Value);
        else
            template = await _context.OnboardingTemplates
                .Include(t => t.Tasks)
                .Where(t => t.IsActive && (t.DepartmentId == employee.DepartmentId || t.IsDefault))
                .OrderByDescending(t => t.DepartmentId == employee.DepartmentId)
                .ThenByDescending(t => t.IsDefault)
                .FirstOrDefaultAsync();

        if (template == null)
            return BadRequest("No onboarding template found");

        var startDate = employee.JoinedDate ?? DateTime.Today;
        var tasks = template.Tasks.Select(tt => new OnboardingTask
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TemplateTaskId = tt.Id,
            Title = tt.Title,
            Description = tt.Description,
            Category = tt.Category,
            SortOrder = tt.SortOrder,
            DueDate = tt.DueDaysFromStart.HasValue ? startDate.AddDays(tt.DueDaysFromStart.Value) : null,
            IsRequired = tt.IsRequired,
            Status = OnboardingTaskStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        _context.OnboardingTasks.AddRange(tasks);
        await _context.SaveChangesAsync();
        return Ok(new { employeeId, tasksCreated = tasks.Count });
    }

    [HttpPut("tasks/{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.OnboardingTasks.FindAsync(id);
        if (task == null) return NotFound();

        task.Status = request.Status;
        task.Notes = request.Notes;
        if (request.Status == OnboardingTaskStatus.Completed)
            task.CompletedDate = DateTime.UtcNow;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(task);
    }

    // Offboarding
    [HttpGet("offboarding/tasks")]
    public async Task<IActionResult> GetOffboardingTasks([FromQuery] Guid? employeeId)
    {
        var query = _context.OffboardingTasks.Include(t => t.Employee).AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(t => t.EmployeeId == employeeId.Value);

        var tasks = await query
            .OrderBy(t => t.SortOrder)
            .Select(t => new {
                t.Id, t.EmployeeId, EmployeeName = t.Employee.FullName,
                t.Title, t.Description, t.Category, t.DueDate,
                t.Status, t.IsRequired, t.CompletedDate
            })
            .ToListAsync();
        return Ok(tasks);
    }

    [HttpPost("offboarding/initiate/{employeeId}")]
    public async Task<IActionResult> InitiateOffboarding(Guid employeeId, [FromBody] InitiateOffboardingRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return NotFound("Employee not found");

        var terminationDate = request.TerminationDate ?? DateTime.Today.AddDays(30);
        
        // Create standard offboarding tasks
        var standardTasks = new[]
        {
            new { Title = "Return Company Equipment", Category = "Equipment", DueDays = -1 },
            new { Title = "Revoke System Access", Category = "Access", DueDays = 0 },
            new { Title = "Knowledge Transfer", Category = "Handover", DueDays = -7 },
            new { Title = "Exit Interview", Category = "HR", DueDays = -3 },
            new { Title = "Final Settlement Calculation", Category = "Finance", DueDays = 0 },
            new { Title = "Experience Letter", Category = "Documentation", DueDays = 3 },
            new { Title = "Update Employee Records", Category = "HR", DueDays = 0 }
        };

        var tasks = standardTasks.Select((t, i) => new OffboardingTask
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Title = t.Title,
            Category = t.Category,
            SortOrder = i,
            DueDate = terminationDate.AddDays(t.DueDays),
            Status = OffboardingTaskStatus.Pending,
            IsRequired = true,
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        _context.OffboardingTasks.AddRange(tasks);
        
        employee.Status = EmployeeStatus.Notice;
        employee.TerminationDate = terminationDate;
        employee.TerminationReason = request.Reason;
        employee.UpdatedAtUtc = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { employeeId, terminationDate, tasksCreated = tasks.Count });
    }

    // Exit Interviews
    [HttpGet("exit-interviews")]
    public async Task<IActionResult> GetExitInterviews()
    {
        var interviews = await _context.ExitInterviews
            .Include(e => e.Employee)
            .OrderByDescending(e => e.InterviewDate)
            .Select(e => new {
                e.Id, e.EmployeeId, EmployeeName = e.Employee.FullName,
                e.InterviewDate, e.PrimaryReasonForLeaving,
                e.OverallSatisfactionRating, e.WouldRecommend, e.WouldRejoin
            })
            .ToListAsync();
        return Ok(interviews);
    }

    [HttpPost("exit-interviews")]
    public async Task<IActionResult> CreateExitInterview([FromBody] CreateExitInterviewRequest request)
    {
        var interview = new ExitInterview
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            InterviewDate = request.InterviewDate,
            InterviewerId = request.InterviewerId,
            SeparationReason = request.SeparationReason,
            PrimaryReasonForLeaving = request.PrimaryReasonForLeaving,
            OverallSatisfactionRating = request.OverallSatisfactionRating,
            ManagementRating = request.ManagementRating,
            WorkEnvironmentRating = request.WorkEnvironmentRating,
            CompensationRating = request.CompensationRating,
            GrowthOpportunitiesRating = request.GrowthOpportunitiesRating,
            WhatLikedMost = request.WhatLikedMost,
            WhatLikedLeast = request.WhatLikedLeast,
            Suggestions = request.Suggestions,
            WouldRecommend = request.WouldRecommend,
            WouldRejoin = request.WouldRejoin,
            AdditionalComments = request.AdditionalComments,
            IsConfidential = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ExitInterviews.Add(interview);
        await _context.SaveChangesAsync();
        return Ok(interview);
    }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateTemplateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int SortOrder { get; set; }
    public int? DueDaysFromStart { get; set; }
    public string? AssigneeRole { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class UpdateTaskRequest
{
    public OnboardingTaskStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class InitiateOffboardingRequest
{
    public DateTime? TerminationDate { get; set; }
    public string? Reason { get; set; }
}

public class CreateExitInterviewRequest
{
    public Guid EmployeeId { get; set; }
    public DateTime InterviewDate { get; set; }
    public Guid? InterviewerId { get; set; }
    public string? SeparationReason { get; set; }
    public string? PrimaryReasonForLeaving { get; set; }
    public int? OverallSatisfactionRating { get; set; }
    public int? ManagementRating { get; set; }
    public int? WorkEnvironmentRating { get; set; }
    public int? CompensationRating { get; set; }
    public int? GrowthOpportunitiesRating { get; set; }
    public string? WhatLikedMost { get; set; }
    public string? WhatLikedLeast { get; set; }
    public string? Suggestions { get; set; }
    public bool? WouldRecommend { get; set; }
    public bool? WouldRejoin { get; set; }
    public string? AdditionalComments { get; set; }
}
