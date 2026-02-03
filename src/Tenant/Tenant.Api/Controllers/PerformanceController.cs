using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly TenantDbContext _context;

    public PerformanceController(TenantDbContext context)
    {
        _context = context;
    }

    // Reviews
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews([FromQuery] Guid? employeeId)
    {
        var query = _context.PerformanceReviews
            .Include(r => r.Employee)
            .Include(r => r.Reviewer)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        var reviews = await query
            .OrderByDescending(r => r.ReviewDate)
            .Select(r => new {
                r.Id, r.EmployeeId, EmployeeName = r.Employee.FullName,
                r.ReviewerId, ReviewerName = r.Reviewer != null ? r.Reviewer.FullName : null,
                r.ReviewPeriod, r.ReviewDate, r.DueDate, r.Status, r.OverallRating
            })
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpGet("reviews/{id}")]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var review = await _context.PerformanceReviews
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        return Ok(review);
    }

    [HttpPost("reviews")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        var review = new PerformanceReview
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            ReviewerId = request.ReviewerId,
            ReviewPeriod = request.ReviewPeriod,
            ReviewDate = request.ReviewDate,
            DueDate = request.DueDate,
            Status = ReviewStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.PerformanceReviews.Add(review);
        await _context.SaveChangesAsync();
        return Ok(review);
    }

    [HttpPut("reviews/{id}")]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewRequest request)
    {
        var review = await _context.PerformanceReviews.FindAsync(id);
        if (review == null) return NotFound();

        review.OverallRating = request.OverallRating;
        review.EmployeeSelfReview = request.EmployeeSelfReview;
        review.ManagerReview = request.ManagerReview;
        review.Strengths = request.Strengths;
        review.AreasForImprovement = request.AreasForImprovement;
        review.Goals = request.Goals;
        review.DevelopmentPlan = request.DevelopmentPlan;
        review.Status = request.Status;
        review.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(review);
    }

    // Goals
    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals([FromQuery] Guid? employeeId)
    {
        var query = _context.Goals.Include(g => g.Employee).AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(g => g.EmployeeId == employeeId.Value);

        var goals = await query
            .OrderByDescending(g => g.DueDate)
            .Select(g => new {
                g.Id, g.EmployeeId, EmployeeName = g.Employee.FullName,
                g.Title, g.Description, g.Category, g.Priority,
                g.StartDate, g.DueDate, g.Status, g.ProgressPercent, g.Weight
            })
            .ToListAsync();
        return Ok(goals);
    }

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal([FromBody] CreateGoalRequest request)
    {
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            Status = GoalStatus.NotStarted,
            Weight = request.Weight,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();
        return Ok(goal);
    }

    [HttpPut("goals/{id}")]
    public async Task<IActionResult> UpdateGoal(Guid id, [FromBody] UpdateGoalRequest request)
    {
        var goal = await _context.Goals.FindAsync(id);
        if (goal == null) return NotFound();

        goal.Title = request.Title;
        goal.Description = request.Description;
        goal.Status = request.Status;
        goal.ProgressPercent = request.ProgressPercent;
        goal.Notes = request.Notes;
        goal.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(goal);
    }
}

public class CreateReviewRequest
{
    public Guid EmployeeId { get; set; }
    public Guid? ReviewerId { get; set; }
    public string ReviewPeriod { get; set; } = string.Empty;
    public DateTime ReviewDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateReviewRequest
{
    public decimal? OverallRating { get; set; }
    public string? EmployeeSelfReview { get; set; }
    public string? ManagerReview { get; set; }
    public string? Strengths { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? Goals { get; set; }
    public string? DevelopmentPlan { get; set; }
    public ReviewStatus Status { get; set; }
}

public class CreateGoalRequest
{
    public Guid EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; }
    public GoalPriority Priority { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? Weight { get; set; }
}

public class UpdateGoalRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public string? Notes { get; set; }
}
