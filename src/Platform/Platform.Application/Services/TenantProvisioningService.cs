using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Platform.Application.DTOs;
using Platform.Application.Interfaces;
using Platform.Domain.Entities;
using TenantEntity = Platform.Domain.Entities.Tenant;

namespace Platform.Application.Services;

public class TenantProvisioningService
{
    private readonly ITenantsRepository _repository;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly IEmailService _emailService;
    private readonly TenantSchemaCreator _schemaCreator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ITenantsRepository repository,
        ITenantDbContextFactory tenantDbContextFactory,
        IEmailService emailService,
        TenantSchemaCreator schemaCreator,
        IConfiguration configuration,
        ILogger<TenantProvisioningService> logger)
    {
        _repository = repository;
        _tenantDbContextFactory = tenantDbContextFactory;
        _emailService = emailService;
        _schemaCreator = schemaCreator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RegisterTenantResponse> CreateTenantAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenantCode = GenerateTenantCode(request.CompanyName);
        
        // Validate tenant code doesn't exist
        var existingTenant = await _repository.GetByTenantCodeAsync(tenantCode, cancellationToken);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"A tenant with code '{tenantCode}' already exists. Please try a different company name.");
        }

        // Validate email doesn't exist
        var emailExists = await CheckEmailExistsInAnyTenantAsync(request.AdminEmail.ToLowerInvariant(), cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException($"The email address '{request.AdminEmail}' is already registered.");
        }

        var dbName = $"tenant_{tenantCode.ToLowerInvariant()}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var tenant = new TenantEntity
        {
            TenantId = Guid.NewGuid(),
            TenantCode = tenantCode,
            CompanyName = request.CompanyName,
            LegalName = request.LegalName,
            Country = request.Country,
            TimeZone = request.TimeZone,
            Currency = request.Currency,
            AdminEmail = request.AdminEmail.ToLowerInvariant(),
            DbName = dbName,
            DbHost = _configuration["Database:Host"] ?? "localhost",
            DbPort = int.TryParse(_configuration["Database:Port"], out var port) ? port : 5432,
            Status = TenantStatus.Provisioning,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Create tenant record first
        await _repository.CreateAsync(tenant, cancellationToken);
        _logger.LogInformation("Created tenant record: {TenantCode}", tenantCode);

        // Create provisioning job
        var job = new TenantProvisioningJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Status = ProvisioningStatus.InProgress,
            StartedAtUtc = DateTime.UtcNow
        };
        await _repository.CreateProvisioningJobAsync(job, cancellationToken);

        try
        {
            // Create the database
            await CreateDatabaseAsync(dbName, cancellationToken);
            _logger.LogInformation("Created database: {DbName}", dbName);
            await Task.Delay(1000, cancellationToken);

            // Get connection string and create all tables using comprehensive schema
            var connectionString = GetTenantConnectionString(dbName);
            await _schemaCreator.CreateAllTablesAsync(connectionString, cancellationToken);
            _logger.LogInformation("Created comprehensive HR schema for tenant: {TenantCode}", tenantCode);
            await Task.Delay(500, cancellationToken);

            // Create admin user
            await CreateCompanyAdminAsync(connectionString, request.AdminEmail.ToLowerInvariant(), request.AdminPassword, request.AdminFullName ?? "Admin", cancellationToken);
            _logger.LogInformation("Created admin user for tenant: {TenantCode}", tenantCode);

            // Update tenant status
            tenant.Status = TenantStatus.Active;
            tenant.UpdatedAtUtc = DateTime.UtcNow;
            await _repository.UpdateAsync(tenant, cancellationToken);

            // Update job status
            job.Status = ProvisioningStatus.Completed;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _repository.UpdateProvisioningJobAsync(job, cancellationToken);

            // Send email
            var tenantUrl = $"http://localhost:3000/login?tenant={tenantCode}";
            await _emailService.SendTenantRegistrationEmailAsync(request.AdminEmail, tenantCode, request.CompanyName, tenantUrl, cancellationToken);

            return new RegisterTenantResponse
            {
                TenantId = tenant.TenantId,
                TenantCode = tenantCode,
                CompanyName = request.CompanyName,
                TenantUrl = tenantUrl,
                Message = "Tenant created successfully. Check your email for login details."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision tenant: {TenantCode}", tenantCode);
            
            job.Status = ProvisioningStatus.Failed;
            job.LastError = ex.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _repository.UpdateProvisioningJobAsync(job, cancellationToken);

            tenant.Status = TenantStatus.Suspended;
            tenant.UpdatedAtUtc = DateTime.UtcNow;
            await _repository.UpdateAsync(tenant, cancellationToken);

            throw;
        }
    }

    private string GenerateTenantCode(string companyName)
    {
        var cleanName = new string(companyName.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
        if (cleanName.Length > 8) cleanName = cleanName[..8];
        var random = new Random().Next(1000, 9999);
        return $"{cleanName}{random}";
    }

    private async Task<bool> CheckEmailExistsInAnyTenantAsync(string email, CancellationToken cancellationToken)
    {
        var tenants = await _repository.GetAllAsync(cancellationToken);
        foreach (var tenant in tenants.Where(t => t.Status == TenantStatus.Active))
        {
            try
            {
                var connectionString = GetTenantConnectionString(tenant.DbName);
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                
                await using var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM \"Users\" WHERE LOWER(\"Email\") = LOWER(@email)", connection);
                cmd.Parameters.AddWithValue("email", email);
                
                var count = (long)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
                if (count > 0) return true;
            }
            catch
            {
                // Ignore errors for individual tenant checks
            }
        }
        return false;
    }

    private async Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken)
    {
        var masterConnectionString = GetMasterConnectionString();
        await using var connection = new NpgsqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Check if database exists
        await using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'", connection);
        var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);

        if (exists == null)
        {
            await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", connection);
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task CreateTablesUsingRawSql(string connectionString, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating comprehensive HR SaaS tenant schema...");
        
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // =============================================
        // IDENTITY & ACCESS MANAGEMENT
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Roles"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL UNIQUE,
                ""Description"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL
            );", cancellationToken);
        
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" UUID PRIMARY KEY,
                ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                ""PasswordHash"" VARCHAR(500) NOT NULL,
                ""FullName"" VARCHAR(200) NOT NULL,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""EmailConfirmed"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""AccessFailedCount"" INTEGER NOT NULL DEFAULT 0,
                ""LockoutEndUtc"" TIMESTAMP,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);
        
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""UserRoles"" (
                ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                ""RoleId"" UUID NOT NULL REFERENCES ""Roles""(""Id"") ON DELETE CASCADE,
                ""AssignedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
                PRIMARY KEY (""UserId"", ""RoleId"")
            );", cancellationToken);
        
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Permissions"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL UNIQUE,
                ""Description"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL
            );", cancellationToken);
        
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""RolePermissions"" (
                ""RoleId"" UUID NOT NULL REFERENCES ""Roles""(""Id"") ON DELETE CASCADE,
                ""PermissionId"" UUID NOT NULL REFERENCES ""Permissions""(""Id"") ON DELETE CASCADE,
                ""GrantedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
                PRIMARY KEY (""RoleId"", ""PermissionId"")
            );", cancellationToken);
        
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""RefreshTokens"" (
                ""Id"" UUID PRIMARY KEY,
                ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                ""Token"" TEXT NOT NULL,
                ""ExpiresAtUtc"" TIMESTAMP NOT NULL,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""RevokedAtUtc"" TIMESTAMP,
                ""RevokedReason"" TEXT
            );", cancellationToken);

        // =============================================
        // ORGANIZATION STRUCTURE
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Departments"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Code"" VARCHAR(50),
                ""ParentId"" UUID REFERENCES ""Departments""(""Id""),
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Designations"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Code"" VARCHAR(50),
                ""Level"" INTEGER,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Branches"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Code"" VARCHAR(50),
                ""Address"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // EMPLOYEES
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Employees"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeCode"" VARCHAR(50) NOT NULL UNIQUE,
                ""FullName"" VARCHAR(200) NOT NULL,
                ""FirstName"" VARCHAR(100),
                ""LastName"" VARCHAR(100),
                ""NicOrPassport"" VARCHAR(50),
                ""DateOfBirth"" DATE,
                ""Gender"" VARCHAR(20),
                ""MaritalStatus"" VARCHAR(20),
                ""Nationality"" VARCHAR(50),
                ""Address"" TEXT,
                ""City"" VARCHAR(100),
                ""State"" VARCHAR(100),
                ""Country"" VARCHAR(100),
                ""PostalCode"" VARCHAR(20),
                ""Phone"" VARCHAR(50),
                ""PersonalEmail"" VARCHAR(255),
                ""Email"" VARCHAR(255),
                ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
                ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
                ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
                ""ReportsToId"" UUID REFERENCES ""Employees""(""Id""),
                ""JoinedDate"" DATE,
                ""ConfirmationDate"" DATE,
                ""TerminationDate"" DATE,
                ""TerminationReason"" TEXT,
                ""EmploymentType"" VARCHAR(50),
                ""WorkLocation"" VARCHAR(100),
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""UserId"" UUID REFERENCES ""Users""(""Id""),
                ""ProfilePhotoPath"" TEXT,
                ""Bio"" TEXT,
                ""Skills"" TEXT,
                ""BankName"" VARCHAR(100),
                ""BankAccountNumber"" VARCHAR(50),
                ""BankRoutingNumber"" VARCHAR(50),
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""EmployeeDocuments"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""DocumentType"" VARCHAR(50) NOT NULL,
                ""FileName"" VARCHAR(255) NOT NULL,
                ""FilePath"" TEXT,
                ""MimeType"" VARCHAR(100),
                ""FileSize"" BIGINT,
                ""Description"" TEXT,
                ""ExpiryDate"" DATE,
                ""IsVerified"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""VerifiedBy"" UUID,
                ""VerifiedAtUtc"" TIMESTAMP,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""EmergencyContacts"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""Name"" VARCHAR(200) NOT NULL,
                ""Relationship"" VARCHAR(50) NOT NULL,
                ""Phone"" VARCHAR(50) NOT NULL,
                ""AlternatePhone"" VARCHAR(50),
                ""Email"" VARCHAR(255),
                ""Address"" TEXT,
                ""IsPrimary"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""EmployeeHistory"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""ChangeType"" VARCHAR(50) NOT NULL,
                ""FieldName"" VARCHAR(100),
                ""OldValue"" TEXT,
                ""NewValue"" TEXT,
                ""EffectiveDate"" DATE NOT NULL,
                ""Reason"" TEXT,
                ""Notes"" TEXT,
                ""ChangedBy"" UUID,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Compensations"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""BaseSalary"" DECIMAL(18,2) NOT NULL,
                ""Currency"" VARCHAR(10) NOT NULL DEFAULT 'USD',
                ""PayFrequency"" VARCHAR(20) NOT NULL DEFAULT 'Monthly',
                ""HousingAllowance"" DECIMAL(18,2),
                ""TransportAllowance"" DECIMAL(18,2),
                ""MealAllowance"" DECIMAL(18,2),
                ""OtherAllowances"" DECIMAL(18,2),
                ""Bonus"" DECIMAL(18,2),
                ""EffectiveDate"" DATE NOT NULL,
                ""EndDate"" DATE,
                ""Notes"" TEXT,
                ""IsCurrent"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""JobInfos"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""JobTitle"" VARCHAR(200) NOT NULL,
                ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
                ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
                ""ReportsToId"" UUID REFERENCES ""Employees""(""Id""),
                ""EmploymentType"" VARCHAR(50),
                ""WorkLocation"" VARCHAR(100),
                ""EffectiveDate"" DATE NOT NULL,
                ""EndDate"" DATE,
                ""Notes"" TEXT,
                ""IsCurrent"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // ATTENDANCE & TIME MANAGEMENT
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""ShiftTemplates"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Code"" VARCHAR(50),
                ""StartTime"" TIME NOT NULL,
                ""EndTime"" TIME NOT NULL,
                ""BreakDuration"" INTERVAL,
                ""WorkingHours"" DECIMAL(4,2) NOT NULL,
                ""IsNightShift"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""IsFlexible"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""GracePeriodMinutes"" INTEGER,
                ""EarlyCheckInMinutes"" INTEGER,
                ""Color"" VARCHAR(20),
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""EmployeeRosters"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""ShiftTemplateId"" UUID NOT NULL REFERENCES ""ShiftTemplates""(""Id""),
                ""Date"" DATE NOT NULL,
                ""CustomStartTime"" TIME,
                ""CustomEndTime"" TIME,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""AttendanceLogs"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""Timestamp"" TIMESTAMP NOT NULL,
                ""LogType"" INTEGER NOT NULL,
                ""DeviceId"" VARCHAR(100),
                ""DeviceName"" VARCHAR(100),
                ""Location"" VARCHAR(200),
                ""Latitude"" DECIMAL(10,7),
                ""Longitude"" DECIMAL(10,7),
                ""IpAddress"" VARCHAR(50),
                ""Notes"" TEXT,
                ""IsManualEntry"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""ApprovedBy"" UUID,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""AttendanceRecords"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""Date"" DATE NOT NULL,
                ""CheckInTime"" TIME,
                ""CheckOutTime"" TIME,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // LEAVE MANAGEMENT
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""LeaveTypes"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Code"" VARCHAR(50),
                ""DefaultDays"" DECIMAL(5,2) NOT NULL,
                ""IsPaid"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""LeavePolicies"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Description"" TEXT,
                ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
                ""AnnualAllocation"" DECIMAL(5,2) NOT NULL,
                ""AccrualMethod"" INTEGER NOT NULL DEFAULT 0,
                ""MaxCarryForward"" DECIMAL(5,2),
                ""MaxAccumulation"" DECIMAL(5,2),
                ""MinServiceDaysRequired"" INTEGER,
                ""MinNoticeDays"" INTEGER,
                ""MaxConsecutiveDays"" INTEGER,
                ""AllowNegativeBalance"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""RequiresApproval"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""RequiresDocument"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""DocumentRequiredAfterDays"" INTEGER,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""LeaveBalances"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
                ""Year"" INTEGER NOT NULL,
                ""Allocated"" DECIMAL(5,2) NOT NULL DEFAULT 0,
                ""Used"" DECIMAL(5,2) NOT NULL DEFAULT 0,
                ""Pending"" DECIMAL(5,2) NOT NULL DEFAULT 0,
                ""CarriedForward"" DECIMAL(5,2) NOT NULL DEFAULT 0,
                ""Adjusted"" DECIMAL(5,2) NOT NULL DEFAULT 0,
                ""LastAccrualDate"" DATE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP,
                UNIQUE(""EmployeeId"", ""LeaveTypeId"", ""Year"")
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""LeaveRequests"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
                ""StartDate"" DATE NOT NULL,
                ""EndDate"" DATE NOT NULL,
                ""Reason"" TEXT,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""LeaveApprovals"" (
                ""Id"" UUID PRIMARY KEY,
                ""LeaveRequestId"" UUID NOT NULL REFERENCES ""LeaveRequests""(""Id"") ON DELETE CASCADE,
                ""ApproverId"" UUID NOT NULL,
                ""ApprovalLevel"" INTEGER NOT NULL DEFAULT 1,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Comments"" TEXT,
                ""ActionDate"" TIMESTAMP,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL
            );", cancellationToken);

        // =============================================
        // PERFORMANCE MANAGEMENT
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""PerformanceReviews"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""ReviewerId"" UUID REFERENCES ""Employees""(""Id""),
                ""ReviewPeriod"" VARCHAR(50) NOT NULL,
                ""ReviewDate"" DATE NOT NULL,
                ""DueDate"" DATE,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""OverallRating"" DECIMAL(3,2),
                ""EmployeeSelfReview"" TEXT,
                ""ManagerReview"" TEXT,
                ""Strengths"" TEXT,
                ""AreasForImprovement"" TEXT,
                ""Goals"" TEXT,
                ""DevelopmentPlan"" TEXT,
                ""EmployeeComments"" TEXT,
                ""EmployeeAcknowledgedAt"" TIMESTAMP,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Goals"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""Title"" VARCHAR(200) NOT NULL,
                ""Description"" TEXT,
                ""Category"" INTEGER NOT NULL DEFAULT 0,
                ""Priority"" INTEGER NOT NULL DEFAULT 1,
                ""StartDate"" DATE NOT NULL,
                ""DueDate"" DATE NOT NULL,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""ProgressPercent"" INTEGER NOT NULL DEFAULT 0,
                ""Metrics"" TEXT,
                ""Notes"" TEXT,
                ""ParentGoalId"" UUID REFERENCES ""Goals""(""Id""),
                ""Weight"" DECIMAL(5,2),
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // RECRUITING / ATS
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""JobPostings"" (
                ""Id"" UUID PRIMARY KEY,
                ""Title"" VARCHAR(200) NOT NULL,
                ""JobCode"" VARCHAR(50),
                ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
                ""Location"" VARCHAR(200),
                ""EmploymentType"" VARCHAR(50) NOT NULL DEFAULT 'FullTime',
                ""ExperienceLevel"" VARCHAR(50),
                ""Description"" TEXT,
                ""Requirements"" TEXT,
                ""Responsibilities"" TEXT,
                ""Benefits"" TEXT,
                ""SalaryMin"" DECIMAL(18,2),
                ""SalaryMax"" DECIMAL(18,2),
                ""Currency"" VARCHAR(10),
                ""ShowSalary"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Openings"" INTEGER,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""PublishedAt"" TIMESTAMP,
                ""ClosingDate"" DATE,
                ""HiringManagerId"" UUID,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Applicants"" (
                ""Id"" UUID PRIMARY KEY,
                ""JobPostingId"" UUID NOT NULL REFERENCES ""JobPostings""(""Id"") ON DELETE CASCADE,
                ""FirstName"" VARCHAR(100) NOT NULL,
                ""LastName"" VARCHAR(100) NOT NULL,
                ""Email"" VARCHAR(255) NOT NULL,
                ""Phone"" VARCHAR(50),
                ""ResumePath"" TEXT,
                ""CoverLetterPath"" TEXT,
                ""LinkedInUrl"" TEXT,
                ""PortfolioUrl"" TEXT,
                ""Source"" VARCHAR(50),
                ""ReferredBy"" VARCHAR(200),
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Stage"" INTEGER NOT NULL DEFAULT 0,
                ""Rating"" INTEGER,
                ""Notes"" TEXT,
                ""ExpectedSalary"" DECIMAL(18,2),
                ""NoticePeriod"" VARCHAR(50),
                ""AppliedAt"" TIMESTAMP NOT NULL,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Interviews"" (
                ""Id"" UUID PRIMARY KEY,
                ""ApplicantId"" UUID NOT NULL REFERENCES ""Applicants""(""Id"") ON DELETE CASCADE,
                ""InterviewType"" VARCHAR(50) NOT NULL,
                ""ScheduledAt"" TIMESTAMP NOT NULL,
                ""DurationMinutes"" INTEGER NOT NULL DEFAULT 60,
                ""Location"" VARCHAR(200),
                ""MeetingLink"" TEXT,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""InterviewerIds"" TEXT,
                ""OverallRating"" INTEGER,
                ""Feedback"" TEXT,
                ""Strengths"" TEXT,
                ""Weaknesses"" TEXT,
                ""Recommendation"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // BENEFITS
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""BenefitPlans"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Code"" VARCHAR(50),
                ""Type"" INTEGER NOT NULL,
                ""Description"" TEXT,
                ""Provider"" VARCHAR(100),
                ""EmployerContribution"" DECIMAL(18,2),
                ""EmployeeContribution"" DECIMAL(18,2),
                ""ContributionType"" VARCHAR(20),
                ""EligibilityCriteria"" TEXT,
                ""WaitingPeriodDays"" INTEGER,
                ""EnrollmentStartDate"" DATE,
                ""EnrollmentEndDate"" DATE,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""EmployeeBenefits"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""BenefitPlanId"" UUID NOT NULL REFERENCES ""BenefitPlans""(""Id""),
                ""EnrollmentDate"" DATE NOT NULL,
                ""EffectiveDate"" DATE,
                ""TerminationDate"" DATE,
                ""CoverageLevel"" VARCHAR(50),
                ""EmployeeContribution"" DECIMAL(18,2),
                ""EmployerContribution"" DECIMAL(18,2),
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // TRAINING
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Trainings"" (
                ""Id"" UUID PRIMARY KEY,
                ""Title"" VARCHAR(200) NOT NULL,
                ""Code"" VARCHAR(50),
                ""Description"" TEXT,
                ""Type"" INTEGER NOT NULL DEFAULT 0,
                ""Category"" VARCHAR(50),
                ""Provider"" VARCHAR(100),
                ""Instructor"" VARCHAR(100),
                ""DurationHours"" INTEGER,
                ""Cost"" DECIMAL(18,2),
                ""Currency"" VARCHAR(10),
                ""Location"" VARCHAR(200),
                ""OnlineUrl"" TEXT,
                ""MaxParticipants"" INTEGER,
                ""IsMandatory"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""HasCertification"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""ValidityMonths"" INTEGER,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""EmployeeTrainings"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""TrainingId"" UUID NOT NULL REFERENCES ""Trainings""(""Id""),
                ""AssignedDate"" DATE,
                ""DueDate"" DATE,
                ""StartDate"" DATE,
                ""CompletedDate"" DATE,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""ProgressPercent"" INTEGER,
                ""Score"" DECIMAL(5,2),
                ""Passed"" BOOLEAN,
                ""CertificateNumber"" VARCHAR(100),
                ""CertificateExpiryDate"" DATE,
                ""Feedback"" TEXT,
                ""Rating"" INTEGER,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // ONBOARDING & OFFBOARDING
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""OnboardingTemplates"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Description"" TEXT,
                ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
                ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
                ""IsDefault"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""OnboardingTemplateTasks"" (
                ""Id"" UUID PRIMARY KEY,
                ""OnboardingTemplateId"" UUID NOT NULL REFERENCES ""OnboardingTemplates""(""Id"") ON DELETE CASCADE,
                ""Title"" VARCHAR(200) NOT NULL,
                ""Description"" TEXT,
                ""Category"" VARCHAR(50),
                ""SortOrder"" INTEGER NOT NULL DEFAULT 0,
                ""DueDaysFromStart"" INTEGER,
                ""AssigneeRole"" VARCHAR(50),
                ""IsRequired"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""OnboardingTasks"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""TemplateTaskId"" UUID REFERENCES ""OnboardingTemplateTasks""(""Id""),
                ""Title"" VARCHAR(200) NOT NULL,
                ""Description"" TEXT,
                ""Category"" VARCHAR(50),
                ""SortOrder"" INTEGER NOT NULL DEFAULT 0,
                ""DueDate"" DATE,
                ""AssignedToId"" UUID,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CompletedDate"" DATE,
                ""CompletedById"" UUID,
                ""Notes"" TEXT,
                ""IsRequired"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""OffboardingTasks"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""Title"" VARCHAR(200) NOT NULL,
                ""Description"" TEXT,
                ""Category"" VARCHAR(50),
                ""SortOrder"" INTEGER NOT NULL DEFAULT 0,
                ""DueDate"" DATE,
                ""AssignedToId"" UUID,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CompletedDate"" DATE,
                ""CompletedById"" UUID,
                ""Notes"" TEXT,
                ""IsRequired"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""ExitInterviews"" (
                ""Id"" UUID PRIMARY KEY,
                ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
                ""InterviewDate"" DATE NOT NULL,
                ""InterviewerId"" UUID,
                ""SeparationReason"" TEXT,
                ""PrimaryReasonForLeaving"" TEXT,
                ""OverallSatisfactionRating"" INTEGER,
                ""ManagementRating"" INTEGER,
                ""WorkEnvironmentRating"" INTEGER,
                ""CompensationRating"" INTEGER,
                ""GrowthOpportunitiesRating"" INTEGER,
                ""WhatLikedMost"" TEXT,
                ""WhatLikedLeast"" TEXT,
                ""Suggestions"" TEXT,
                ""WouldRecommend"" BOOLEAN,
                ""WouldRejoin"" BOOLEAN,
                ""AdditionalComments"" TEXT,
                ""IsConfidential"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        // =============================================
        // COMPANY SETTINGS & UTILITIES
        // =============================================
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Holidays"" (
                ""Id"" UUID PRIMARY KEY,
                ""Name"" VARCHAR(100) NOT NULL,
                ""Date"" DATE NOT NULL,
                ""Type"" INTEGER NOT NULL DEFAULT 0,
                ""IsRecurring"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Description"" TEXT,
                ""ApplicableBranches"" TEXT,
                ""ApplicableDepartments"" TEXT,
                ""IsOptional"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""Announcements"" (
                ""Id"" UUID PRIMARY KEY,
                ""Title"" VARCHAR(200) NOT NULL,
                ""Content"" TEXT NOT NULL,
                ""Type"" INTEGER NOT NULL DEFAULT 0,
                ""Priority"" INTEGER NOT NULL DEFAULT 1,
                ""PublishDate"" TIMESTAMP NOT NULL,
                ""ExpiryDate"" TIMESTAMP,
                ""TargetAudience"" TEXT,
                ""RequiresAcknowledgment"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""IsPinned"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""CreatedById"" UUID,
                ""IsPublished"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);

        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS ""CompanySettings"" (
                ""Id"" UUID PRIMARY KEY,
                ""SettingKey"" VARCHAR(100) NOT NULL UNIQUE,
                ""SettingValue"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL,
                ""UpdatedAtUtc"" TIMESTAMP
            );", cancellationToken);
        
        _logger.LogInformation("Successfully created comprehensive HR SaaS tenant schema with all tables");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateCompanyAdminAsync(string connectionString, string email, string password, string fullName, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get or create CompanyAdmin role
        Guid roleId;
        await using var getRoleCmd = new NpgsqlCommand(@"
            SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'CompanyAdmin' LIMIT 1;", connection);
        var existingRoleId = await getRoleCmd.ExecuteScalarAsync(cancellationToken);
        
        if (existingRoleId != null && existingRoleId != DBNull.Value)
        {
            roleId = (Guid)existingRoleId;
            _logger.LogInformation("Found existing CompanyAdmin role: {RoleId}", roleId);
        }
        else
        {
            // Role doesn't exist, create it
            roleId = Guid.NewGuid();
            await using var roleCmd = new NpgsqlCommand(@"
                INSERT INTO ""Roles"" (""Id"", ""Name"", ""Description"", ""IsSystem"", ""CreatedAtUtc"")
                VALUES (@id, 'CompanyAdmin', 'Company Administrator with full access', TRUE, @createdAt);", connection);
            roleCmd.Parameters.AddWithValue("id", roleId);
            roleCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            await roleCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created CompanyAdmin role: {RoleId}", roleId);
        }

        // Check if user already exists
        Guid userId;
        await using var checkUserCmd = new NpgsqlCommand(@"
            SELECT ""Id"" FROM ""Users"" WHERE ""Email"" = @email LIMIT 1;", connection);
        checkUserCmd.Parameters.AddWithValue("email", email);
        var existingUserId = await checkUserCmd.ExecuteScalarAsync(cancellationToken);
        
        if (existingUserId != null && existingUserId != DBNull.Value)
        {
            userId = (Guid)existingUserId;
            _logger.LogInformation("User {Email} already exists, using existing user: {UserId}", email, userId);
        }
        else
        {
            // Create admin user
            userId = Guid.NewGuid();
            var passwordHash = HashPassword(password);
            
            await using var userCmd = new NpgsqlCommand(@"
                INSERT INTO ""Users"" (""Id"", ""Email"", ""PasswordHash"", ""FullName"", ""IsActive"", ""EmailConfirmed"", ""AccessFailedCount"", ""CreatedAtUtc"")
                VALUES (@id, @email, @passwordHash, @fullName, TRUE, TRUE, 0, @createdAt);", connection);
            userCmd.Parameters.AddWithValue("id", userId);
            userCmd.Parameters.AddWithValue("email", email);
            userCmd.Parameters.AddWithValue("passwordHash", passwordHash);
            userCmd.Parameters.AddWithValue("fullName", fullName);
            userCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            await userCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created admin user: {Email} ({UserId})", email, userId);
        }

        // Check if role is already assigned
        await using var checkUserRoleCmd = new NpgsqlCommand(@"
            SELECT 1 FROM ""UserRoles"" WHERE ""UserId"" = @userId AND ""RoleId"" = @roleId LIMIT 1;", connection);
        checkUserRoleCmd.Parameters.AddWithValue("userId", userId);
        checkUserRoleCmd.Parameters.AddWithValue("roleId", roleId);
        var roleExists = await checkUserRoleCmd.ExecuteScalarAsync(cancellationToken);
        
        if (roleExists == null)
        {
            // Assign role to user
            await using var userRoleCmd = new NpgsqlCommand(@"
                INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAtUtc"")
                VALUES (@userId, @roleId, @assignedAt);", connection);
            userRoleCmd.Parameters.AddWithValue("userId", userId);
            userRoleCmd.Parameters.AddWithValue("roleId", roleId);
            userRoleCmd.Parameters.AddWithValue("assignedAt", DateTime.UtcNow);
            await userRoleCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Assigned CompanyAdmin role to user {Email}", email);
        }
        else
        {
            _logger.LogInformation("User {Email} already has CompanyAdmin role", email);
        }

        _logger.LogInformation("Created admin user {Email} with CompanyAdmin role", email);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var salt = _configuration["Jwt:Salt"] ?? "default-salt";
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GetMasterConnectionString()
    {
        var host = _configuration["Database:Host"] ?? "localhost";
        var port = _configuration["Database:Port"] ?? "5432";
        var user = _configuration["Database:User"] ?? "sa";
        var password = _configuration["Database:Password"] ?? "sa";
        return $"Host={host};Port={port};Database=postgres;Username={user};Password={password}";
    }

    private string GetTenantConnectionString(string dbName)
    {
        var host = _configuration["Database:Host"] ?? "localhost";
        var port = _configuration["Database:Port"] ?? "5432";
        var user = _configuration["Database:User"] ?? "sa";
        var password = _configuration["Database:Password"] ?? "sa";
        return $"Host={host};Port={port};Database={dbName};Username={user};Password={password}";
    }
}
