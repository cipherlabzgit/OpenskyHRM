namespace Tenant.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? NicOrPassport { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? PersonalEmail { get; set; }
    public string? Email { get; set; } // Work email
    
    // Organization
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? ReportsToId { get; set; }
    public Employee? ReportsTo { get; set; }
    
    // Employment
    public DateTime? JoinedDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public string? EmploymentType { get; set; } // FullTime, PartTime, Contract, Intern
    public string EmploymentStatus { get; set; } = "Active"; // Active, Inactive, Terminated, OnLeave
    public string? WorkLocation { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    
    // Linked User Account
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    // Profile
    public string? ProfilePhotoPath { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Bio { get; set; }
    public string? Skills { get; set; }
    
    // Bank Details
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    
    // Navigation properties
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<EmployeeHistory> History { get; set; } = new List<EmployeeHistory>();
    public ICollection<Compensation> Compensations { get; set; } = new List<Compensation>();
    public ICollection<JobInfo> JobHistory { get; set; } = new List<JobInfo>();
    public ICollection<EmployeeRoster> Rosters { get; set; } = new List<EmployeeRoster>();
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<PerformanceReview> PerformanceReviews { get; set; } = new List<PerformanceReview>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<EmployeeBenefit> Benefits { get; set; } = new List<EmployeeBenefit>();
    public ICollection<EmployeeTraining> Trainings { get; set; } = new List<EmployeeTraining>();
    public ICollection<OnboardingTask> OnboardingTasks { get; set; } = new List<OnboardingTask>();
    public ICollection<OffboardingTask> OffboardingTasks { get; set; } = new List<OffboardingTask>();
}

public enum EmployeeStatus
{
    Active = 0,
    Inactive = 1,
    Terminated = 2,
    OnLeave = 3,
    Probation = 4,
    Notice = 5
}
