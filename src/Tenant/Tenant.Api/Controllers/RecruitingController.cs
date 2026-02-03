using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;
using ApplicationEntity = Tenant.Domain.Entities.Application;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/recruitment")]
[Authorize]
public class RecruitingController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<RecruitingController> _logger;

    public RecruitingController(TenantDbContext context, ILogger<RecruitingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Job Requisitions

    [HttpPost("requisitions")]
    public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateRequisition called. DTO: {Dto}", JsonSerializer.Serialize(dto));
        
        try
        {
            var requestedById = GetCurrentUserId();
            _logger.LogInformation("Current user ID from claims: {UserId}", requestedById);
            if (!requestedById.HasValue)
            {
                _logger.LogWarning("User ID not found in claims. User: {User}", User.Identity?.Name);
                return Unauthorized(new { error = "User authentication required. Please log in again." });
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { error = "Title is required" });
            }

            // Validate department and designation if provided
            if (dto.DepartmentId.HasValue)
            {
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value, cancellationToken);
                if (!departmentExists)
                {
                    return BadRequest(new { error = $"Department with ID {dto.DepartmentId.Value} does not exist" });
                }
            }

            if (dto.DesignationId.HasValue)
            {
                var designationExists = await _context.Designations.AnyAsync(d => d.Id == dto.DesignationId.Value, cancellationToken);
                if (!designationExists)
                {
                    return BadRequest(new { error = $"Designation with ID {dto.DesignationId.Value} does not exist" });
                }
            }

            var requisitionNumber = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            
            var requisition = new JobRequisition
            {
                Id = Guid.NewGuid(),
                RequisitionNumber = requisitionNumber,
                Title = dto.Title.Trim(),
                DepartmentId = dto.DepartmentId,
                DesignationId = dto.DesignationId,
                BranchId = dto.BranchId,
                Location = dto.Location,
                EmploymentType = dto.EmploymentType ?? "FullTime",
                Openings = dto.Openings ?? 1,
                BudgetMin = dto.BudgetMin,
                BudgetMax = dto.BudgetMax,
                Currency = dto.Currency ?? "USD",
                Description = dto.Description,
                Requirements = dto.Requirements,
                Responsibilities = dto.Responsibilities,
                RequiredSkills = dto.RequiredSkills,
                PreferredSkills = dto.PreferredSkills,
                MinExperienceYears = dto.MinExperienceYears,
                MaxExperienceYears = dto.MaxExperienceYears,
                EducationLevel = dto.EducationLevel,
                Status = RequisitionStatus.Draft,
                RequestedById = requestedById.Value,
                HiringManagerId = dto.HiringManagerId,
                TargetStartDate = dto.TargetStartDate,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.JobRequisitions.Add(requisition);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Requisition created successfully: {RequisitionNumber} by user {UserId}", requisitionNumber, requestedById);
            return Ok(new { requisition.Id, requisition.RequisitionNumber, message = "Requisition created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating requisition. DTO: {Dto}", JsonSerializer.Serialize(dto));
            return StatusCode(500, new { error = $"Failed to create requisition: {ex.Message}" });
        }
    }

    [HttpGet("requisitions")]
    public async Task<IActionResult> GetRequisitions(
        [FromQuery] RequisitionStatus? status,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobRequisitions
            .Include(r => r.Department)
            .Include(r => r.Designation)
            .Where(r => !r.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (departmentId.HasValue)
            query = query.Where(r => r.DepartmentId == departmentId.Value);

        var total = await query.CountAsync(cancellationToken);
        var requisitions = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.RequisitionNumber,
                r.Title,
                Department = r.Department != null ? r.Department.Name : null,
                Designation = r.Designation != null ? r.Designation.Name : null,
                r.Location,
                r.EmploymentType,
                r.Openings,
                r.Status,
                ApplicationCount = r.Applications.Count,
                r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new { data = requisitions, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("requisitions/{id}")]
    public async Task<IActionResult> GetRequisition(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await _context.JobRequisitions
            .Include(r => r.Department)
            .Include(r => r.Designation)
            .Include(r => r.Branch)
            .Include(r => r.Approvals)
            .Include(r => r.Applications)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (requisition == null)
            return NotFound(new { error = "Requisition not found" });

        return Ok(requisition);
    }

    [HttpPut("requisitions/{id}")]
    public async Task<IActionResult> UpdateRequisition(Guid id, [FromBody] UpdateRequisitionDto dto, CancellationToken cancellationToken)
    {
        var requisition = await _context.JobRequisitions.FindAsync(new object[] { id }, cancellationToken);
        if (requisition == null || requisition.IsDeleted)
            return NotFound(new { error = "Requisition not found" });

        if (requisition.Status != RequisitionStatus.Draft && requisition.Status != RequisitionStatus.PendingApproval)
            return BadRequest(new { error = "Cannot update requisition in current status" });

        requisition.Title = dto.Title ?? requisition.Title;
        requisition.DepartmentId = dto.DepartmentId ?? requisition.DepartmentId;
        requisition.DesignationId = dto.DesignationId ?? requisition.DesignationId;
        requisition.Location = dto.Location ?? requisition.Location;
        requisition.BudgetMin = dto.BudgetMin ?? requisition.BudgetMin;
        requisition.BudgetMax = dto.BudgetMax ?? requisition.BudgetMax;
        requisition.Description = dto.Description ?? requisition.Description;
        requisition.Requirements = dto.Requirements ?? requisition.Requirements;
        requisition.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Requisition updated successfully" });
    }

    [HttpPost("requisitions/{id}/submit")]
    public async Task<IActionResult> SubmitRequisition(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await _context.JobRequisitions.FindAsync(new object[] { id }, cancellationToken);
        if (requisition == null || requisition.IsDeleted)
            return NotFound(new { error = "Requisition not found" });

        if (requisition.Status != RequisitionStatus.Draft)
            return BadRequest(new { error = "Requisition is not in draft status" });

        requisition.Status = RequisitionStatus.PendingApproval;
        requisition.UpdatedAtUtc = DateTime.UtcNow;

        // Create approval records
        // Try to find a department manager or hiring manager for the department
        Guid? approverId = null;
        
        if (requisition.DepartmentId.HasValue)
        {
            // Find users with DepartmentManager or HiringManager role
            var departmentManagers = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => 
                    ur.Role.Name == "DepartmentManager" || ur.Role.Name == "HiringManager"))
                .FirstOrDefaultAsync(cancellationToken);
            
            if (departmentManagers != null)
            {
                approverId = departmentManagers.Id;
            }
        }
        
        // Fallback to hiring manager if specified, or current user
        if (!approverId.HasValue)
        {
            approverId = requisition.HiringManagerId;
        }
        
        // Last fallback to current user
        if (!approverId.HasValue)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                approverId = currentUserId;
            }
        }
        
        if (!approverId.HasValue)
        {
            return BadRequest(new { error = "No approver found. Please create a user with DepartmentManager or HiringManager role, or assign a hiring manager to the requisition." });
        }

        var approval = new JobRequisitionApproval
        {
            Id = Guid.NewGuid(),
            RequisitionId = requisition.Id,
            ApproverId = approverId.Value,
            ApprovalLevel = 1,
            Status = RequisitionApprovalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.JobRequisitionApprovals.Add(approval);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Requisition submitted for approval" });
    }

    [HttpPost("requisitions/{id}/approve")]
    public async Task<IActionResult> ApproveRequisition(Guid id, [FromBody] ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        var requisition = await _context.JobRequisitions
            .Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (requisition == null)
            return NotFound(new { error = "Requisition not found" });

        var approval = requisition.Approvals.FirstOrDefault(a => a.Status == RequisitionApprovalStatus.Pending);
        if (approval == null)
            return BadRequest(new { error = "No pending approval found" });

        approval.Status = RequisitionApprovalStatus.Approved;
        approval.Comments = dto.Comments;
        approval.ApprovedAt = DateTime.UtcNow;

        // If all approvals done, mark as approved
        if (requisition.Approvals.All(a => a.Status == RequisitionApprovalStatus.Approved))
        {
            requisition.Status = RequisitionStatus.Approved;
            requisition.ApprovedAt = DateTime.UtcNow;
        }

        requisition.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Requisition approved" });
    }

    [HttpPost("requisitions/{id}/publish")]
    public async Task<IActionResult> PublishRequisition(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await _context.JobRequisitions.FindAsync(new object[] { id }, cancellationToken);
        if (requisition == null || requisition.IsDeleted)
            return NotFound(new { error = "Requisition not found" });

        if (requisition.Status != RequisitionStatus.Approved)
            return BadRequest(new { error = "Requisition must be approved before publishing" });

        requisition.Status = RequisitionStatus.Published;
        requisition.PublishedAt = DateTime.UtcNow;
        requisition.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Requisition published successfully" });
    }

    #endregion

    #region Candidates

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] string? search,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Candidates
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                c.Email.Contains(search) ||
                (c.CurrentCompany != null && c.CurrentCompany.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(c => c.Source == source);

        var total = await query.CountAsync(cancellationToken);
        var candidates = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Phone,
                c.CurrentCompany,
                c.CurrentTitle,
                c.Source,
                c.Rating,
                ApplicationCount = c.Applications.Count,
                c.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new { data = candidates, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("candidates/{id}")]
    public async Task<IActionResult> GetCandidate(Guid id, CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates
            .Include(c => c.Applications)
                .ThenInclude(a => a.Requisition)
            .Include(c => c.Documents)
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (candidate == null)
            return NotFound(new { error = "Candidate not found" });

        return Ok(candidate);
    }

    [HttpPost("candidates")]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateDto dto, CancellationToken cancellationToken)
    {
        // Check for duplicates
        var existing = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email.ToLower() && !c.IsDeleted, cancellationToken);

        if (existing != null)
            return Conflict(new { error = "Candidate with this email already exists", candidateId = existing.Id });

        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLower(),
            Phone = dto.Phone,
            AlternatePhone = dto.AlternatePhone,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Country = dto.Country,
            PostalCode = dto.PostalCode,
            LinkedInUrl = dto.LinkedInUrl,
            PortfolioUrl = dto.PortfolioUrl,
            CurrentCompany = dto.CurrentCompany,
            CurrentTitle = dto.CurrentTitle,
            CurrentSalary = dto.CurrentSalary,
            ExpectedSalary = dto.ExpectedSalary,
            NoticePeriod = dto.NoticePeriod,
            Source = dto.Source ?? "Manual",
            ReferredByEmployeeId = dto.ReferredByEmployeeId,
            ReferralCode = dto.ReferralCode,
            Tags = dto.Tags,
            Notes = dto.Notes,
            EmailHash = HashString(dto.Email),
            PhoneHash = !string.IsNullOrEmpty(dto.Phone) ? HashString(dto.Phone) : null,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { candidate.Id, message = "Candidate created successfully" });
    }

    [HttpPut("candidates/{id}")]
    public async Task<IActionResult> UpdateCandidate(Guid id, [FromBody] UpdateCandidateDto dto, CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates.FindAsync(new object[] { id }, cancellationToken);
        if (candidate == null || candidate.IsDeleted)
            return NotFound(new { error = "Candidate not found" });

        candidate.FirstName = dto.FirstName ?? candidate.FirstName;
        candidate.LastName = dto.LastName ?? candidate.LastName;
        candidate.Phone = dto.Phone ?? candidate.Phone;
        candidate.CurrentCompany = dto.CurrentCompany ?? candidate.CurrentCompany;
        candidate.CurrentTitle = dto.CurrentTitle ?? candidate.CurrentTitle;
        candidate.ExpectedSalary = dto.ExpectedSalary ?? candidate.ExpectedSalary;
        candidate.Tags = dto.Tags ?? candidate.Tags;
        candidate.Notes = dto.Notes ?? candidate.Notes;
        candidate.Rating = dto.Rating ?? candidate.Rating;
        candidate.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Candidate updated successfully" });
    }

    #endregion

    #region Applications

    [HttpPost("applications")]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationDto dto, CancellationToken cancellationToken)
    {
        // Check if candidate exists, if not create
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email.ToLower() && !c.IsDeleted, cancellationToken);

        if (candidate == null)
        {
            candidate = new Candidate
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLower(),
                Phone = dto.Phone,
                Source = dto.Source ?? "Career Portal",
                EmailHash = HashString(dto.Email),
                PhoneHash = !string.IsNullOrEmpty(dto.Phone) ? HashString(dto.Phone) : null,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Check if application already exists
        var existingApp = await _context.Applications
            .FirstOrDefaultAsync(a => a.CandidateId == candidate.Id && a.RequisitionId == dto.RequisitionId, cancellationToken);

        if (existingApp != null)
            return Conflict(new { error = "Application already exists", applicationId = existingApp.Id });

        var application = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            CandidateId = candidate.Id,
            RequisitionId = dto.RequisitionId,
            CoverLetter = dto.CoverLetter,
            ScreeningAnswers = dto.ScreeningAnswers != null ? JsonSerializer.Serialize(dto.ScreeningAnswers) : null,
            Stage = ApplicationStage.Applied,
            Status = ApplicationStatus.New,
            AppliedAt = DateTime.UtcNow,
            Source = dto.Source ?? "Career Portal",
            ReferralCode = dto.ReferralCode,
            UtmSource = dto.UtmSource,
            UtmMedium = dto.UtmMedium,
            UtmCampaign = dto.UtmCampaign,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Applications.Add(application);

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Type = ActivityType.Applied,
            Title = "Application Submitted",
            Description = $"Applied for position via {application.Source}",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { application.Id, candidateId = candidate.Id, message = "Application submitted successfully" });
    }

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] Guid? requisitionId,
        [FromQuery] ApplicationStage? stage,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .AsQueryable();

        if (requisitionId.HasValue)
            query = query.Where(a => a.RequisitionId == requisitionId.Value);

        if (stage.HasValue)
            query = query.Where(a => a.Stage == stage.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var applications = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                CandidateName = a.Candidate.FirstName + " " + a.Candidate.LastName,
                CandidateEmail = a.Candidate.Email,
                JobTitle = a.Requisition.Title,
                a.Stage,
                a.Status,
                a.AppliedAt,
                InterviewCount = a.Interviews.Count,
                OfferCount = a.Offers.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(new { data = applications, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("applications/{id}")]
    public async Task<IActionResult> GetApplication(Guid id, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .Include(a => a.Interviews)
            .Include(a => a.Offers)
            .Include(a => a.Activities)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application == null)
            return NotFound(new { error = "Application not found" });

        return Ok(application);
    }

    [HttpPut("applications/{id}/stage")]
    public async Task<IActionResult> UpdateApplicationStage(Guid id, [FromBody] UpdateApplicationStageDto dto, CancellationToken cancellationToken)
    {
        var application = await _context.Applications.FindAsync(new object[] { id }, cancellationToken);
        if (application == null)
            return NotFound(new { error = "Application not found" });

        var oldStage = application.Stage;
        var oldStatus = application.Status;

        application.Stage = dto.Stage;
        application.Status = dto.Status;
        application.UpdatedAtUtc = DateTime.UtcNow;

        if (dto.Stage == ApplicationStage.Shortlisted)
            application.ShortlistedAt = DateTime.UtcNow;
        else if (dto.Status == ApplicationStatus.Rejected)
            application.RejectedAt = DateTime.UtcNow;

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Type = oldStage != dto.Stage ? ActivityType.StageChanged : ActivityType.StatusChanged,
            Title = $"Stage changed to {dto.Stage}",
            Description = dto.Notes,
            PerformedById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Application stage updated" });
    }

    #endregion

    #region Interviews

    [HttpPost("interviews")]
    public async Task<IActionResult> CreateInterview([FromBody] CreateInterviewDto dto, CancellationToken cancellationToken)
    {
        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = dto.ApplicationId,
            InterviewType = dto.InterviewType,
            InterviewRound = dto.InterviewRound ?? "Round 1",
            ScheduledAt = dto.ScheduledAt,
            DurationMinutes = dto.DurationMinutes ?? 60,
            Location = dto.Location,
            MeetingLink = dto.MeetingLink,
            MeetingId = dto.MeetingId,
            MeetingPassword = dto.MeetingPassword,
            InterviewerIds = dto.InterviewerIds != null ? JsonSerializer.Serialize(dto.InterviewerIds) : null,
            PanelMembers = dto.PanelMembers != null ? JsonSerializer.Serialize(dto.PanelMembers) : null,
            Agenda = dto.Agenda,
            Status = InterviewStatus.Scheduled,
            ScheduledById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Interviews.Add(interview);

        // Update application stage
        var application = await _context.Applications.FindAsync(new object[] { dto.ApplicationId }, cancellationToken);
        if (application != null)
        {
            application.Stage = ApplicationStage.Interview;
            application.Status = ApplicationStatus.Interviewing;
            application.UpdatedAtUtc = DateTime.UtcNow;
        }

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = dto.ApplicationId,
            Type = ActivityType.InterviewScheduled,
            Title = $"Interview scheduled - {dto.InterviewType}",
            Description = $"Scheduled for {dto.ScheduledAt:g}",
            PerformedById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { interview.Id, message = "Interview scheduled successfully" });
    }

    [HttpGet("interviews")]
    public async Task<IActionResult> GetInterviews(
        [FromQuery] Guid? applicationId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
            .AsQueryable();

        if (applicationId.HasValue)
            query = query.Where(i => i.ApplicationId == applicationId.Value);

        if (startDate.HasValue)
            query = query.Where(i => i.ScheduledAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.ScheduledAt <= endDate.Value);

        var interviews = await query
            .OrderBy(i => i.ScheduledAt)
            .Select(i => new
            {
                i.Id,
                i.ApplicationId,
                CandidateName = i.Application.Candidate.FirstName + " " + i.Application.Candidate.LastName,
                i.InterviewType,
                i.InterviewRound,
                i.ScheduledAt,
                i.DurationMinutes,
                i.Location,
                i.Status,
                i.OverallRating
            })
            .ToListAsync(cancellationToken);

        return Ok(interviews);
    }

    [HttpPost("interviews/{id}/feedback")]
    public async Task<IActionResult> SubmitInterviewFeedback(Guid id, [FromBody] InterviewFeedbackDto dto, CancellationToken cancellationToken)
    {
        var interview = await _context.Interviews.FindAsync(new object[] { id }, cancellationToken);
        if (interview == null)
            return NotFound(new { error = "Interview not found" });

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized(new { error = "User not authenticated" });

        var feedback = new InterviewFeedback
        {
            Id = Guid.NewGuid(),
            InterviewId = id,
            InterviewerId = currentUserId.Value,
            OverallRating = dto.OverallRating,
            TechnicalScore = dto.TechnicalScore,
            CommunicationScore = dto.CommunicationScore,
            CulturalFitScore = dto.CulturalFitScore,
            ProblemSolvingScore = dto.ProblemSolvingScore,
            Strengths = dto.Strengths,
            Weaknesses = dto.Weaknesses,
            OverallComments = dto.OverallComments,
            Recommendation = dto.Recommendation,
            RecommendationNotes = dto.RecommendationNotes,
            SubmittedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.InterviewFeedbacks.Add(feedback);

        // Update interview
        interview.Status = InterviewStatus.Completed;
        interview.CompletedAt = DateTime.UtcNow;
        interview.OverallRating = dto.OverallRating;
        interview.Feedback = dto.OverallComments;
        interview.Strengths = dto.Strengths;
        interview.Weaknesses = dto.Weaknesses;
        interview.Recommendation = dto.Recommendation.ToString();
        interview.UpdatedAtUtc = DateTime.UtcNow;

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = interview.ApplicationId,
            Type = ActivityType.InterviewCompleted,
            Title = "Interview feedback submitted",
            Description = $"Rating: {dto.OverallRating}/10, Recommendation: {dto.Recommendation}",
            PerformedById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Feedback submitted successfully" });
    }

    #endregion

    #region Assessments

    [HttpPost("assessments")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentDto dto, CancellationToken cancellationToken)
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            CandidateId = dto.CandidateId,
            ApplicationId = dto.ApplicationId,
            AssessmentName = dto.AssessmentName,
            AssessmentType = dto.AssessmentType,
            Instructions = dto.Instructions,
            Questions = dto.Questions != null ? JsonSerializer.Serialize(dto.Questions) : null,
            MaxScore = dto.MaxScore,
            PassingScore = dto.PassingScore,
            Status = AssessmentStatus.Assigned,
            AssignedAt = DateTime.UtcNow,
            DueDate = dto.DueDate,
            AssignedById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Assessments.Add(assessment);

        // Update application
        if (dto.ApplicationId.HasValue)
        {
            var application = await _context.Applications.FindAsync(new object[] { dto.ApplicationId.Value }, cancellationToken);
            if (application != null)
            {
                application.Stage = ApplicationStage.Assessment;
                application.Status = ApplicationStatus.AssessmentPending;
                application.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        // Add activity
        if (dto.ApplicationId.HasValue)
        {
            var activity = new ApplicationActivity
            {
                Id = Guid.NewGuid(),
                ApplicationId = dto.ApplicationId.Value,
                Type = ActivityType.AssessmentAssigned,
                Title = $"Assessment assigned: {dto.AssessmentName}",
                Description = dto.Instructions,
                PerformedById = GetCurrentUserId(),
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.ApplicationActivities.Add(activity);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { assessment.Id, message = "Assessment assigned successfully" });
    }

    [HttpPost("assessments/{id}/submit")]
    public async Task<IActionResult> SubmitAssessment(Guid id, [FromBody] SubmitAssessmentDto dto, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments.FindAsync(new object[] { id }, cancellationToken);
        if (assessment == null)
            return NotFound(new { error = "Assessment not found" });

        assessment.Answers = dto.Answers != null ? JsonSerializer.Serialize(dto.Answers) : null;
        assessment.StartedAt = assessment.StartedAt ?? DateTime.UtcNow;
        assessment.CompletedAt = DateTime.UtcNow;
        assessment.Status = AssessmentStatus.Completed;
        assessment.UpdatedAtUtc = DateTime.UtcNow;

        // Calculate score (simplified - in production, use proper scoring logic)
        if (dto.Score.HasValue)
        {
            assessment.Score = dto.Score.Value;
            assessment.IsPassed = assessment.PassingScore.HasValue && dto.Score.Value >= assessment.PassingScore.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Assessment submitted successfully" });
    }

    #endregion

    #region Offers

    [HttpGet("offers")]
    public async Task<IActionResult> GetOffers(
        [FromQuery] Guid? applicationId,
        [FromQuery] OfferStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Offers
            .Include(o => o.Application)
                .ThenInclude(a => a.Candidate)
            .AsQueryable();

        if (applicationId.HasValue)
            query = query.Where(o => o.ApplicationId == applicationId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var offers = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new
            {
                o.Id,
                o.OfferNumber,
                ApplicationId = o.ApplicationId,
                CandidateName = o.Application.Candidate.FirstName + " " + o.Application.Candidate.LastName,
                o.BaseSalary,
                o.Currency,
                o.JoiningDate,
                o.Status,
                o.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(offers);
    }

    [HttpPost("offers")]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferDto dto, CancellationToken cancellationToken)
    {
        var offerNumber = $"OFF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            ApplicationId = dto.ApplicationId,
            RequisitionId = dto.RequisitionId,
            OfferNumber = offerNumber,
            DesignationId = dto.DesignationId,
            DepartmentId = dto.DepartmentId,
            BranchId = dto.BranchId,
            BaseSalary = dto.BaseSalary,
            Currency = dto.Currency ?? "USD",
            SalaryBreakdown = dto.SalaryBreakdown != null ? JsonSerializer.Serialize(dto.SalaryBreakdown) : null,
            Benefits = dto.Benefits != null ? JsonSerializer.Serialize(dto.Benefits) : null,
            JoiningDate = dto.JoiningDate,
            OfferDate = DateTime.UtcNow,
            ExpiryDate = dto.ExpiryDate ?? DateTime.UtcNow.AddDays(7),
            Status = OfferStatus.Draft,
            OfferLetterTemplate = dto.OfferLetterTemplate,
            Notes = dto.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Offers.Add(offer);

        // Update application
        var application = await _context.Applications.FindAsync(new object[] { dto.ApplicationId }, cancellationToken);
        if (application != null)
        {
            application.Stage = ApplicationStage.Offered;
            application.Status = ApplicationStatus.OfferExtended;
            application.UpdatedAtUtc = DateTime.UtcNow;
        }

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = dto.ApplicationId,
            Type = ActivityType.OfferExtended,
            Title = "Offer extended",
            Description = $"Offer #{offerNumber} created",
            PerformedById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { offer.Id, offer.OfferNumber, message = "Offer created successfully" });
    }

    [HttpPost("offers/{id}/send")]
    public async Task<IActionResult> SendOffer(Guid id, CancellationToken cancellationToken)
    {
        var offer = await _context.Offers
            .Include(o => o.Application)
                .ThenInclude(a => a.Candidate)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (offer == null)
            return NotFound(new { error = "Offer not found" });

        if (offer.Status != OfferStatus.Draft && offer.Status != OfferStatus.PendingApproval)
            return BadRequest(new { error = "Offer cannot be sent in current status" });

        offer.Status = OfferStatus.Sent;
        offer.SentAt = DateTime.UtcNow;
        offer.UpdatedAtUtc = DateTime.UtcNow;

        // Log email (in production, send actual email)
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            ToEmail = offer.Application.Candidate.Email,
            ToName = $"{offer.Application.Candidate.FirstName} {offer.Application.Candidate.LastName}",
            Subject = $"Job Offer - {offer.Application.Requisition.Title}",
            Body = "Your offer letter is attached.",
            EmailType = "OfferExtended",
            RelatedEntityId = offer.Id,
            RelatedEntityType = "Offer",
            Status = EmailStatus.Sent,
            SentAt = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.EmailLogs.Add(emailLog);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Offer sent successfully" });
    }

    [HttpPost("offers/{id}/accept")]
    public async Task<IActionResult> AcceptOffer(Guid id, [FromBody] AcceptOfferDto dto, CancellationToken cancellationToken)
    {
        var offer = await _context.Offers
            .Include(o => o.Application)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound(new { error = "Offer not found" });

        if (offer.Status != OfferStatus.Sent)
            return BadRequest(new { error = "Offer is not in sent status" });

        offer.Status = OfferStatus.Accepted;
        offer.AcceptedAt = DateTime.UtcNow;
        offer.AcceptanceNotes = dto.Notes;
        offer.UpdatedAtUtc = DateTime.UtcNow;

        // Update application
        var application = offer.Application;
        application.Status = ApplicationStatus.OfferAccepted;
        application.UpdatedAtUtc = DateTime.UtcNow;

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Type = ActivityType.OfferAccepted,
            Title = "Offer accepted",
            Description = dto.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Offer accepted successfully" });
    }

    [HttpPost("offers/{id}/convert-to-employee")]
    public async Task<IActionResult> ConvertToEmployee(Guid id, [FromBody] ConvertToEmployeeDto dto, CancellationToken cancellationToken)
    {
        var offer = await _context.Offers
            .Include(o => o.Application)
                .ThenInclude(a => a.Candidate)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (offer == null)
            return NotFound(new { error = "Offer not found" });

        if (offer.Status != OfferStatus.Accepted)
            return BadRequest(new { error = "Offer must be accepted before converting to employee" });

        // Create employee from candidate
        var employeeCode = $"EMP{DateTime.UtcNow:yyyyMMddHHmmss}";
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            FirstName = offer.Application.Candidate.FirstName,
            LastName = offer.Application.Candidate.LastName,
            FullName = $"{offer.Application.Candidate.FirstName} {offer.Application.Candidate.LastName}",
            Email = offer.Application.Candidate.Email,
            Phone = offer.Application.Candidate.Phone,
            PersonalEmail = offer.Application.Candidate.Email,
            DepartmentId = offer.DepartmentId,
            DesignationId = offer.DesignationId,
            BranchId = offer.BranchId,
            JoinedDate = offer.JoiningDate ?? DateTime.Today,
            EmploymentType = "FullTime",
            EmploymentStatus = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Employees.Add(employee);

        // Update offer
        offer.Status = OfferStatus.Converted;
        offer.ConvertedToEmployeeId = employee.Id;
        offer.ConvertedAt = DateTime.UtcNow;
        offer.UpdatedAtUtc = DateTime.UtcNow;

        // Update application
        var application = offer.Application;
        application.Stage = ApplicationStage.Hired;
        application.Status = ApplicationStatus.Hired;
        application.UpdatedAtUtc = DateTime.UtcNow;

        // Add activity
        var activity = new ApplicationActivity
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Type = ActivityType.OfferAccepted,
            Title = "Converted to employee",
            Description = $"Employee created with code: {employeeCode}",
            PerformedById = GetCurrentUserId(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ApplicationActivities.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { employeeId = employee.Id, employeeCode, message = "Successfully converted to employee" });
    }

    #endregion

    #region Analytics & Reports

    [HttpGet("analytics/pipeline")]
    public async Task<IActionResult> GetPipelineAnalytics(
        [FromQuery] Guid? requisitionId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Applications.AsQueryable();

        if (requisitionId.HasValue)
            query = query.Where(a => a.RequisitionId == requisitionId.Value);

        if (startDate.HasValue)
            query = query.Where(a => a.AppliedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.AppliedAt <= endDate.Value);

        var pipeline = await query
            .GroupBy(a => a.Stage)
            .Select(g => new
            {
                Stage = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return Ok(pipeline);
    }

    [HttpGet("analytics/sources")]
    public async Task<IActionResult> GetSourceAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Applications.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.AppliedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.AppliedAt <= endDate.Value);

        var sources = await query
            .GroupBy(a => a.Source ?? "Unknown")
            .Select(g => new
            {
                Source = g.Key,
                Count = g.Count(),
                HiredCount = g.Count(a => a.Status == ApplicationStatus.Hired)
            })
            .OrderByDescending(s => s.Count)
            .ToListAsync(cancellationToken);

        return Ok(sources);
    }

    [HttpGet("analytics/time-to-hire")]
    public async Task<IActionResult> GetTimeToHire(
        [FromQuery] Guid? requisitionId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Applications
            .Where(a => a.Status == ApplicationStatus.Hired)
            .AsQueryable();

        if (requisitionId.HasValue)
            query = query.Where(a => a.RequisitionId == requisitionId.Value);

        if (startDate.HasValue)
            query = query.Where(a => a.AppliedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.AppliedAt <= endDate.Value);

        var applications = await query.ToListAsync(cancellationToken);

        var timeToHire = applications
            .Where(a => a.AppliedAt != default && a.UpdatedAtUtc.HasValue)
            .Select(a => (a.UpdatedAtUtc!.Value - a.AppliedAt).TotalDays)
            .ToList();

        var avgTimeToHire = timeToHire.Any() ? timeToHire.Average() : 0;

        return Ok(new
        {
            averageDays = Math.Round(avgTimeToHire, 2),
            minDays = timeToHire.Any() ? Math.Round(timeToHire.Min(), 2) : 0,
            maxDays = timeToHire.Any() ? Math.Round(timeToHire.Max(), 2) : 0,
            totalHired = applications.Count
        });
    }

    #endregion

    #region Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string HashString(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}

#region DTOs

public class CreateRequisitionDto
{
    public string Title { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public int? Openings { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    public string? RequiredSkills { get; set; }
    public string? PreferredSkills { get; set; }
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }
    public string? EducationLevel { get; set; }
    public Guid? HiringManagerId { get; set; }
    public DateTime? TargetStartDate { get; set; }
}

public class UpdateRequisitionDto
{
    public string? Title { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public string? Location { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
}

public class ApprovalActionDto
{
    public string? Comments { get; set; }
}

public class CreateCandidateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentTitle { get; set; }
    public decimal? CurrentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? NoticePeriod { get; set; }
    public string? Source { get; set; }
    public Guid? ReferredByEmployeeId { get; set; }
    public string? ReferralCode { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCandidateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentTitle { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public int? Rating { get; set; }
}

public class CreateApplicationDto
{
    public Guid RequisitionId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CoverLetter { get; set; }
    public Dictionary<string, string>? ScreeningAnswers { get; set; }
    public string? Source { get; set; }
    public string? ReferralCode { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
}

public class UpdateApplicationStageDto
{
    public ApplicationStage Stage { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class CreateInterviewDto
{
    public Guid ApplicationId { get; set; }
    public string InterviewType { get; set; } = string.Empty;
    public string? InterviewRound { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string? MeetingId { get; set; }
    public string? MeetingPassword { get; set; }
    public List<Guid>? InterviewerIds { get; set; }
    public List<string>? PanelMembers { get; set; }
    public string? Agenda { get; set; }
}

public class InterviewFeedbackDto
{
    public int? OverallRating { get; set; }
    public string? TechnicalScore { get; set; }
    public string? CommunicationScore { get; set; }
    public string? CulturalFitScore { get; set; }
    public string? ProblemSolvingScore { get; set; }
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    public string? OverallComments { get; set; }
    public Recommendation Recommendation { get; set; }
    public string? RecommendationNotes { get; set; }
}

public class CreateAssessmentDto
{
    public Guid CandidateId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public Dictionary<string, object>? Questions { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? PassingScore { get; set; }
    public DateTime? DueDate { get; set; }
}

public class SubmitAssessmentDto
{
    public Dictionary<string, object>? Answers { get; set; }
    public decimal? Score { get; set; }
}

public class CreateOfferDto
{
    public Guid ApplicationId { get; set; }
    public Guid? RequisitionId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
    public decimal BaseSalary { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, decimal>? SalaryBreakdown { get; set; }
    public Dictionary<string, object>? Benefits { get; set; }
    public DateTime? JoiningDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? OfferLetterTemplate { get; set; }
    public string? Notes { get; set; }
}

public class AcceptOfferDto
{
    public string? Notes { get; set; }
}

public class ConvertToEmployeeDto
{
    public string? EmployeeCode { get; set; }
    public string? Notes { get; set; }
}

#endregion
