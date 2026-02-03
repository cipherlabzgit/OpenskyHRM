using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly TenantDbContext _context;

    public AnnouncementsController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? published)
    {
        var query = _context.Announcements.AsQueryable();
        if (published.HasValue)
            query = query.Where(a => a.IsPublished == published.Value);

        var announcements = await query
            .Where(a => a.ExpiryDate == null || a.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishDate)
            .Select(a => new {
                a.Id, a.Title, a.Content, a.Type, a.Priority,
                a.PublishDate, a.ExpiryDate, a.IsPinned, a.IsPublished, a.RequiresAcknowledgment
            })
            .ToListAsync();
        return Ok(announcements);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement == null) return NotFound();
        return Ok(announcement);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request)
    {
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Type = request.Type,
            Priority = request.Priority,
            PublishDate = request.PublishDate ?? DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate,
            TargetAudience = request.TargetAudience,
            RequiresAcknowledgment = request.RequiresAcknowledgment,
            IsPinned = request.IsPinned,
            IsPublished = request.IsPublished,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();
        return Ok(announcement);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateAnnouncementRequest request)
    {
        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement == null) return NotFound();
        
        announcement.Title = request.Title;
        announcement.Content = request.Content;
        announcement.Type = request.Type;
        announcement.Priority = request.Priority;
        announcement.ExpiryDate = request.ExpiryDate;
        announcement.TargetAudience = request.TargetAudience;
        announcement.RequiresAcknowledgment = request.RequiresAcknowledgment;
        announcement.IsPinned = request.IsPinned;
        announcement.IsPublished = request.IsPublished;
        announcement.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(announcement);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement == null) return NotFound();
        _context.Announcements.Remove(announcement);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; }
    public AnnouncementPriority Priority { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? TargetAudience { get; set; }
    public bool RequiresAcknowledgment { get; set; }
    public bool IsPinned { get; set; }
    public bool IsPublished { get; set; }
}
