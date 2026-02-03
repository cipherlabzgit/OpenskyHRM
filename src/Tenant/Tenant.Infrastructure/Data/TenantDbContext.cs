using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using ApplicationEntity = Tenant.Domain.Entities.Application;

namespace Tenant.Infrastructure.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    // Identity & Access
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // Organization
    public DbSet<Department> Departments { get; set; }
    public DbSet<Designation> Designations { get; set; }
    public DbSet<Branch> Branches { get; set; }

    // Employees
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<EmergencyContact> EmergencyContacts { get; set; }
    public DbSet<EmployeeHistory> EmployeeHistory { get; set; }
    public DbSet<Compensation> Compensations { get; set; }
    public DbSet<JobInfo> JobInfos { get; set; }

    // Attendance & Time
    public DbSet<ShiftTemplate> ShiftTemplates { get; set; }
    public DbSet<EmployeeRoster> EmployeeRosters { get; set; }
    public DbSet<AttendanceLog> AttendanceLogs { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    // Leave Management
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeavePolicy> LeavePolicies { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveApproval> LeaveApprovals { get; set; }

    // Performance
    public DbSet<PerformanceReview> PerformanceReviews { get; set; }
    public DbSet<Goal> Goals { get; set; }

    // Recruiting - Legacy (keeping for backward compatibility)
    public DbSet<JobPosting> JobPostings { get; set; }
    public DbSet<Applicant> Applicants { get; set; }
    
    // Recruitment - New Comprehensive Module
    public DbSet<JobRequisition> JobRequisitions { get; set; }
    public DbSet<JobRequisitionApproval> JobRequisitionApprovals { get; set; }
    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<ApplicationEntity> Applications { get; set; }
    public DbSet<CandidateDocument> CandidateDocuments { get; set; }
    public DbSet<ApplicationActivity> ApplicationActivities { get; set; }
    public DbSet<Interview> Interviews { get; set; }
    public DbSet<InterviewFeedback> InterviewFeedbacks { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<Offer> Offers { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }

    // Benefits
    public DbSet<BenefitPlan> BenefitPlans { get; set; }
    public DbSet<EmployeeBenefit> EmployeeBenefits { get; set; }

    // Training
    public DbSet<Training> Trainings { get; set; }
    public DbSet<EmployeeTraining> EmployeeTrainings { get; set; }

    // Onboarding & Offboarding
    public DbSet<OnboardingTemplate> OnboardingTemplates { get; set; }
    public DbSet<OnboardingTemplateTask> OnboardingTemplateTasks { get; set; }
    public DbSet<OnboardingTask> OnboardingTasks { get; set; }
    public DbSet<OffboardingTask> OffboardingTasks { get; set; }
    public DbSet<ExitInterview> ExitInterviews { get; set; }

    // Company
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<CompanySettings> CompanySettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity configurations
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);

        // Employee self-reference
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.ReportsTo)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ReportsToId)
            .OnDelete(DeleteBehavior.Restrict);

        // JobInfo self-reference
        modelBuilder.Entity<JobInfo>()
            .HasOne(j => j.ReportsTo)
            .WithMany()
            .HasForeignKey(j => j.ReportsToId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal self-reference
        modelBuilder.Entity<Goal>()
            .HasOne(g => g.ParentGoal)
            .WithMany()
            .HasForeignKey(g => g.ParentGoalId)
            .OnDelete(DeleteBehavior.Restrict);

        // PerformanceReview relationships
        modelBuilder.Entity<PerformanceReview>()
            .HasOne(pr => pr.Employee)
            .WithMany(e => e.PerformanceReviews)
            .HasForeignKey(pr => pr.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PerformanceReview>()
            .HasOne(pr => pr.Reviewer)
            .WithMany()
            .HasForeignKey(pr => pr.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Recruitment relationships
        modelBuilder.Entity<JobRequisition>()
            .HasOne(jr => jr.RequestedBy)
            .WithMany()
            .HasForeignKey(jr => jr.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobRequisitionApproval>()
            .HasOne(jra => jra.Requisition)
            .WithMany(jr => jr.Approvals)
            .HasForeignKey(jra => jra.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationEntity>()
            .HasOne(a => a.Candidate)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationEntity>()
            .HasOne(a => a.Requisition)
            .WithMany(jr => jr.Applications)
            .HasForeignKey(a => a.RequisitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationActivity>()
            .HasOne(aa => aa.Application)
            .WithMany(a => a.Activities)
            .HasForeignKey(aa => aa.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CandidateDocument>()
            .HasOne(cd => cd.Candidate)
            .WithMany(c => c.Documents)
            .HasForeignKey(cd => cd.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Interview>()
            .HasOne(i => i.Application)
            .WithMany(a => a.Interviews)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InterviewFeedback>()
            .HasOne(ifb => ifb.Interview)
            .WithMany(i => i.Feedbacks)
            .HasForeignKey(ifb => ifb.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Assessment>()
            .HasOne(ass => ass.Candidate)
            .WithMany(c => c.Assessments)
            .HasForeignKey(ass => ass.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Assessment>()
            .HasOne(ass => ass.Application)
            .WithMany()
            .HasForeignKey(ass => ass.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Offer>()
            .HasOne(o => o.Application)
            .WithMany(a => a.Offers)
            .HasForeignKey(o => o.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique indexes
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(p => p.Name).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(d => d.Name);
        modelBuilder.Entity<Designation>().HasIndex(d => d.Name);
        modelBuilder.Entity<JobRequisition>().HasIndex(jr => jr.RequisitionNumber).IsUnique();
        modelBuilder.Entity<Candidate>().HasIndex(c => c.Email);
        modelBuilder.Entity<Offer>().HasIndex(o => o.OfferNumber).IsUnique();
    }
}
