using Microsoft.Extensions.Logging;
using Npgsql;

namespace Platform.Application.Services;

public class TenantSchemaCreator
{
    private readonly ILogger<TenantSchemaCreator> _logger;

    public TenantSchemaCreator(ILogger<TenantSchemaCreator> logger)
    {
        _logger = logger;
    }

    public async Task CreateAllTablesAsync(string connectionString, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating comprehensive tenant database schema...");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Execute schema creation in order (respecting foreign key dependencies)
        await ExecuteSqlAsync(connection, GetIdentitySchema(), "Identity tables", cancellationToken);
        await ExecuteSqlAsync(connection, GetCompanySettingsSchema(), "Company Settings", cancellationToken);
        await ExecuteSqlAsync(connection, GetOrganizationSchema(), "Organization structure", cancellationToken);
        await ExecuteSqlAsync(connection, GetEmployeeSchema(), "Employee tables", cancellationToken);
        await ExecuteSqlAsync(connection, GetAttendanceSchema(), "Attendance tables", cancellationToken);
        await ExecuteSqlAsync(connection, GetLeaveSchema(), "Leave management", cancellationToken);
        await ExecuteSqlAsync(connection, GetPerformanceSchema(), "Performance management", cancellationToken);
        await ExecuteSqlAsync(connection, GetRecruitingSchema(), "Recruiting/ATS", cancellationToken);
        await ExecuteSqlAsync(connection, GetBenefitsSchema(), "Benefits", cancellationToken);
        await ExecuteSqlAsync(connection, GetTrainingSchema(), "Training", cancellationToken);
        await ExecuteSqlAsync(connection, GetOnboardingSchema(), "Onboarding/Offboarding", cancellationToken);
        await ExecuteSqlAsync(connection, GetPayrollSchema(), "Payroll", cancellationToken);
        await ExecuteSqlAsync(connection, GetDocumentSchema(), "Documents", cancellationToken);
        await ExecuteSqlAsync(connection, GetAnnouncementsSchema(), "Announcements", cancellationToken);

        _logger.LogInformation("Tenant database schema created successfully");
    }

    private async Task ExecuteSqlAsync(NpgsqlConnection connection, string sql, string description, CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created {Description}", description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {Description}", description);
            throw;
        }
    }

    private string GetIdentitySchema() => @"
        -- Roles table
        CREATE TABLE IF NOT EXISTS ""Roles"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL UNIQUE,
            ""Description"" TEXT,
            ""IsSystem"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Permissions table
        CREATE TABLE IF NOT EXISTS ""Permissions"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL UNIQUE,
            ""Description"" TEXT,
            ""Module"" VARCHAR(50),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- RolePermissions junction table
        CREATE TABLE IF NOT EXISTS ""RolePermissions"" (
            ""RoleId"" UUID NOT NULL REFERENCES ""Roles""(""Id"") ON DELETE CASCADE,
            ""PermissionId"" UUID NOT NULL REFERENCES ""Permissions""(""Id"") ON DELETE CASCADE,
            ""GrantedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            PRIMARY KEY (""RoleId"", ""PermissionId"")
        );

        -- Users table
        CREATE TABLE IF NOT EXISTS ""Users"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Email"" VARCHAR(255) NOT NULL UNIQUE,
            ""PasswordHash"" VARCHAR(500) NOT NULL,
            ""FullName"" VARCHAR(200) NOT NULL,
            ""AvatarUrl"" TEXT,
            ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""EmailConfirmed"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""PhoneNumber"" VARCHAR(20),
            ""PhoneConfirmed"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""TwoFactorEnabled"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""AccessFailedCount"" INTEGER NOT NULL DEFAULT 0,
            ""LockoutEndUtc"" TIMESTAMP,
            ""LastLoginUtc"" TIMESTAMP,
            ""PasswordChangedAtUtc"" TIMESTAMP,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- UserRoles junction table
        CREATE TABLE IF NOT EXISTS ""UserRoles"" (
            ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
            ""RoleId"" UUID NOT NULL REFERENCES ""Roles""(""Id"") ON DELETE CASCADE,
            ""AssignedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""AssignedBy"" UUID,
            PRIMARY KEY (""UserId"", ""RoleId"")
        );

        -- RefreshTokens table
        CREATE TABLE IF NOT EXISTS ""RefreshTokens"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
            ""Token"" TEXT NOT NULL,
            ""ExpiresAtUtc"" TIMESTAMP NOT NULL,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""RevokedAtUtc"" TIMESTAMP,
            ""RevokedReason"" TEXT,
            ""ReplacedByToken"" TEXT
        );

        -- UserInvitations table
        CREATE TABLE IF NOT EXISTS ""UserInvitations"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Email"" VARCHAR(255) NOT NULL,
            ""RoleId"" UUID REFERENCES ""Roles""(""Id""),
            ""Token"" VARCHAR(100) NOT NULL UNIQUE,
            ""ExpiresAtUtc"" TIMESTAMP NOT NULL,
            ""AcceptedAtUtc"" TIMESTAMP,
            ""InvitedBy"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Insert default roles
        INSERT INTO ""Roles"" (""Id"", ""Name"", ""Description"", ""IsSystem"", ""CreatedAtUtc"") 
        SELECT gen_random_uuid(), 'CompanyAdmin', 'Full system administrator', TRUE, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'CompanyAdmin')
        UNION ALL
        SELECT gen_random_uuid(), 'HRManager', 'HR department manager', TRUE, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'HRManager')
        UNION ALL
        SELECT gen_random_uuid(), 'DepartmentManager', 'Department manager with approval authority', TRUE, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'DepartmentManager')
        UNION ALL
        SELECT gen_random_uuid(), 'HiringManager', 'Hiring manager for recruitment approvals', TRUE, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'HiringManager')
        UNION ALL
        SELECT gen_random_uuid(), 'Manager', 'Department/team manager', TRUE, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'Manager')
        UNION ALL
        SELECT gen_random_uuid(), 'Employee', 'Regular employee', TRUE, NOW()
        WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'Employee');

        -- Insert default permissions
        INSERT INTO ""Permissions"" (""Name"", ""Description"", ""Module"") VALUES 
            ('employees.view', 'View employees', 'Employees'),
            ('employees.create', 'Create employees', 'Employees'),
            ('employees.edit', 'Edit employees', 'Employees'),
            ('employees.delete', 'Delete employees', 'Employees'),
            ('attendance.view', 'View attendance', 'Attendance'),
            ('attendance.manage', 'Manage attendance', 'Attendance'),
            ('leave.view', 'View leave requests', 'Leave'),
            ('leave.request', 'Submit leave requests', 'Leave'),
            ('leave.approve', 'Approve leave requests', 'Leave'),
            ('leave.manage', 'Manage leave settings', 'Leave'),
            ('payroll.view', 'View payroll', 'Payroll'),
            ('payroll.manage', 'Manage payroll', 'Payroll'),
            ('reports.view', 'View reports', 'Reports'),
            ('reports.export', 'Export reports', 'Reports'),
            ('settings.view', 'View settings', 'Settings'),
            ('settings.manage', 'Manage settings', 'Settings'),
            ('recruiting.view', 'View recruiting', 'Recruiting'),
            ('recruiting.manage', 'Manage recruiting', 'Recruiting'),
            ('performance.view', 'View performance', 'Performance'),
            ('performance.manage', 'Manage performance', 'Performance'),
            ('training.view', 'View training', 'Training'),
            ('training.manage', 'Manage training', 'Training')
        ON CONFLICT (""Name"") DO NOTHING;
    ";

    private string GetCompanySettingsSchema() => @"
        -- Company Settings table
        CREATE TABLE IF NOT EXISTS ""CompanySettings"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""CompanyName"" VARCHAR(200) NOT NULL,
            ""LegalName"" VARCHAR(200),
            ""TaxId"" VARCHAR(50),
            ""RegistrationNumber"" VARCHAR(50),
            ""Industry"" VARCHAR(100),
            ""CompanySize"" VARCHAR(50),
            ""Website"" VARCHAR(255),
            ""LogoUrl"" TEXT,
            ""PrimaryColor"" VARCHAR(7),
            ""Address"" TEXT,
            ""City"" VARCHAR(100),
            ""State"" VARCHAR(100),
            ""Country"" VARCHAR(100),
            ""PostalCode"" VARCHAR(20),
            ""Phone"" VARCHAR(20),
            ""Email"" VARCHAR(255),
            ""TimeZone"" VARCHAR(50) DEFAULT 'UTC',
            ""DateFormat"" VARCHAR(20) DEFAULT 'YYYY-MM-DD',
            ""Currency"" VARCHAR(3) DEFAULT 'USD',
            ""FiscalYearStart"" INTEGER DEFAULT 1,
            ""WorkWeekStart"" INTEGER DEFAULT 1,
            ""DefaultWorkHours"" DECIMAL(4,2) DEFAULT 8.0,
            ""OvertimeEnabled"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Company Holidays table
        CREATE TABLE IF NOT EXISTS ""CompanyHolidays"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Date"" DATE NOT NULL,
            ""IsRecurring"" BOOLEAN DEFAULT FALSE,
            ""Description"" TEXT,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Work Schedules table
        CREATE TABLE IF NOT EXISTS ""WorkSchedules"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""IsDefault"" BOOLEAN DEFAULT FALSE,
            ""MondayStart"" TIME,
            ""MondayEnd"" TIME,
            ""TuesdayStart"" TIME,
            ""TuesdayEnd"" TIME,
            ""WednesdayStart"" TIME,
            ""WednesdayEnd"" TIME,
            ""ThursdayStart"" TIME,
            ""ThursdayEnd"" TIME,
            ""FridayStart"" TIME,
            ""FridayEnd"" TIME,
            ""SaturdayStart"" TIME,
            ""SaturdayEnd"" TIME,
            ""SundayStart"" TIME,
            ""SundayEnd"" TIME,
            ""BreakDuration"" INTEGER DEFAULT 60,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );
    ";

    private string GetOrganizationSchema() => @"
        -- Departments table
        CREATE TABLE IF NOT EXISTS ""Departments"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Code"" VARCHAR(20) UNIQUE,
            ""Description"" TEXT,
            ""ParentId"" UUID REFERENCES ""Departments""(""Id""),
            ""HeadId"" UUID,
            ""CostCenter"" VARCHAR(50),
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Designations/Job Titles table
        CREATE TABLE IF NOT EXISTS ""Designations"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Code"" VARCHAR(20) UNIQUE,
            ""Description"" TEXT,
            ""Level"" INTEGER,
            ""MinSalary"" DECIMAL(18,2),
            ""MaxSalary"" DECIMAL(18,2),
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Branches/Locations table
        CREATE TABLE IF NOT EXISTS ""Branches"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Code"" VARCHAR(20) UNIQUE,
            ""Address"" TEXT,
            ""City"" VARCHAR(100),
            ""State"" VARCHAR(100),
            ""Country"" VARCHAR(100),
            ""PostalCode"" VARCHAR(20),
            ""Phone"" VARCHAR(20),
            ""Email"" VARCHAR(255),
            ""ManagerId"" UUID,
            ""TimeZone"" VARCHAR(50),
            ""IsHeadquarters"" BOOLEAN DEFAULT FALSE,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Teams table
        CREATE TABLE IF NOT EXISTS ""Teams"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""LeaderId"" UUID,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );
    ";

    private string GetEmployeeSchema() => @"
        -- Employees table
        CREATE TABLE IF NOT EXISTS ""Employees"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeCode"" VARCHAR(50) NOT NULL UNIQUE,
            ""UserId"" UUID REFERENCES ""Users""(""Id""),
            ""FirstName"" VARCHAR(100) NOT NULL,
            ""MiddleName"" VARCHAR(100),
            ""LastName"" VARCHAR(100) NOT NULL,
            ""FullName"" VARCHAR(300) NOT NULL,
            ""Email"" VARCHAR(255),
            ""PersonalEmail"" VARCHAR(255),
            ""Phone"" VARCHAR(20),
            ""MobilePhone"" VARCHAR(20),
            ""DateOfBirth"" DATE,
            ""Gender"" VARCHAR(20),
            ""MaritalStatus"" VARCHAR(20),
            ""Nationality"" VARCHAR(100),
            ""NationalId"" VARCHAR(50),
            ""PassportNumber"" VARCHAR(50),
            ""PassportExpiry"" DATE,
            ""TaxId"" VARCHAR(50),
            ""SocialSecurityNumber"" VARCHAR(50),
            ""Address"" TEXT,
            ""City"" VARCHAR(100),
            ""State"" VARCHAR(100),
            ""Country"" VARCHAR(100),
            ""PostalCode"" VARCHAR(20),
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
            ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
            ""TeamId"" UUID REFERENCES ""Teams""(""Id""),
            ""ReportsToId"" UUID REFERENCES ""Employees""(""Id""),
            ""WorkScheduleId"" UUID REFERENCES ""WorkSchedules""(""Id""),
            ""EmploymentType"" VARCHAR(50) DEFAULT 'FullTime',
            ""EmploymentStatus"" VARCHAR(50) DEFAULT 'Active',
            ""JoinedDate"" DATE,
            ""ConfirmationDate"" DATE,
            ""ProbationEndDate"" DATE,
            ""ContractEndDate"" DATE,
            ""TerminationDate"" DATE,
            ""TerminationReason"" TEXT,
            ""ProfilePhotoUrl"" TEXT,
            ""Bio"" TEXT,
            ""Skills"" TEXT,
            ""LinkedInUrl"" VARCHAR(255),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Emergency Contacts
        CREATE TABLE IF NOT EXISTS ""EmergencyContacts"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Name"" VARCHAR(200) NOT NULL,
            ""Relationship"" VARCHAR(50),
            ""Phone"" VARCHAR(20) NOT NULL,
            ""AlternatePhone"" VARCHAR(20),
            ""Email"" VARCHAR(255),
            ""Address"" TEXT,
            ""IsPrimary"" BOOLEAN DEFAULT FALSE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Bank Details
        CREATE TABLE IF NOT EXISTS ""EmployeeBankDetails"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""BankName"" VARCHAR(100) NOT NULL,
            ""BranchName"" VARCHAR(100),
            ""AccountNumber"" VARCHAR(50) NOT NULL,
            ""AccountHolderName"" VARCHAR(200),
            ""RoutingNumber"" VARCHAR(50),
            ""SwiftCode"" VARCHAR(20),
            ""IBAN"" VARCHAR(50),
            ""IsPrimary"" BOOLEAN DEFAULT TRUE,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Job History
        CREATE TABLE IF NOT EXISTS ""EmployeeJobHistory"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
            ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
            ""ReportsToId"" UUID REFERENCES ""Employees""(""Id""),
            ""EffectiveDate"" DATE NOT NULL,
            ""EndDate"" DATE,
            ""ChangeType"" VARCHAR(50),
            ""ChangeReason"" TEXT,
            ""Notes"" TEXT,
            ""CreatedBy"" UUID,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Compensation
        CREATE TABLE IF NOT EXISTS ""EmployeeCompensation"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""EffectiveDate"" DATE NOT NULL,
            ""EndDate"" DATE,
            ""SalaryType"" VARCHAR(20) DEFAULT 'Monthly',
            ""BaseSalary"" DECIMAL(18,2) NOT NULL,
            ""Currency"" VARCHAR(3) DEFAULT 'USD',
            ""PayFrequency"" VARCHAR(20) DEFAULT 'Monthly',
            ""HourlyRate"" DECIMAL(10,2),
            ""OvertimeRate"" DECIMAL(10,2),
            ""ChangeReason"" TEXT,
            ""Notes"" TEXT,
            ""CreatedBy"" UUID,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Education
        CREATE TABLE IF NOT EXISTS ""EmployeeEducation"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Institution"" VARCHAR(200) NOT NULL,
            ""Degree"" VARCHAR(100),
            ""FieldOfStudy"" VARCHAR(100),
            ""StartDate"" DATE,
            ""EndDate"" DATE,
            ""Grade"" VARCHAR(20),
            ""Description"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Work Experience
        CREATE TABLE IF NOT EXISTS ""EmployeeWorkExperience"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Company"" VARCHAR(200) NOT NULL,
            ""JobTitle"" VARCHAR(100),
            ""Location"" VARCHAR(200),
            ""StartDate"" DATE,
            ""EndDate"" DATE,
            ""IsCurrent"" BOOLEAN DEFAULT FALSE,
            ""Description"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Dependents
        CREATE TABLE IF NOT EXISTS ""EmployeeDependents"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Name"" VARCHAR(200) NOT NULL,
            ""Relationship"" VARCHAR(50) NOT NULL,
            ""DateOfBirth"" DATE,
            ""Gender"" VARCHAR(20),
            ""Phone"" VARCHAR(20),
            ""IsBeneficiary"" BOOLEAN DEFAULT FALSE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Notes
        CREATE TABLE IF NOT EXISTS ""EmployeeNotes"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Title"" VARCHAR(200),
            ""Content"" TEXT NOT NULL,
            ""IsPrivate"" BOOLEAN DEFAULT TRUE,
            ""CreatedBy"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );
    ";

    private string GetAttendanceSchema() => @"
        -- Shift Templates
        CREATE TABLE IF NOT EXISTS ""ShiftTemplates"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Code"" VARCHAR(20),
            ""StartTime"" TIME NOT NULL,
            ""EndTime"" TIME NOT NULL,
            ""BreakDuration"" INTEGER DEFAULT 60,
            ""GracePeriod"" INTEGER DEFAULT 15,
            ""Color"" VARCHAR(7),
            ""IsNightShift"" BOOLEAN DEFAULT FALSE,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Roster/Schedule
        CREATE TABLE IF NOT EXISTS ""EmployeeRosters"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""ShiftTemplateId"" UUID REFERENCES ""ShiftTemplates""(""Id""),
            ""Date"" DATE NOT NULL,
            ""StartTime"" TIME,
            ""EndTime"" TIME,
            ""Status"" VARCHAR(20) DEFAULT 'Scheduled',
            ""Notes"" TEXT,
            ""CreatedBy"" UUID,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Attendance Raw Logs (from biometric devices, etc.)
        CREATE TABLE IF NOT EXISTS ""AttendanceLogs"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""LogTime"" TIMESTAMP NOT NULL,
            ""LogType"" VARCHAR(20) NOT NULL,
            ""DeviceId"" VARCHAR(50),
            ""DeviceLocation"" VARCHAR(100),
            ""IPAddress"" VARCHAR(50),
            ""Latitude"" DECIMAL(10,8),
            ""Longitude"" DECIMAL(11,8),
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Attendance Records (processed daily records)
        CREATE TABLE IF NOT EXISTS ""AttendanceRecords"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Date"" DATE NOT NULL,
            ""CheckInTime"" TIME,
            ""CheckOutTime"" TIME,
            ""ScheduledStart"" TIME,
            ""ScheduledEnd"" TIME,
            ""WorkedMinutes"" INTEGER,
            ""OvertimeMinutes"" INTEGER DEFAULT 0,
            ""LateMinutes"" INTEGER DEFAULT 0,
            ""EarlyLeaveMinutes"" INTEGER DEFAULT 0,
            ""BreakMinutes"" INTEGER DEFAULT 0,
            ""Status"" VARCHAR(20) DEFAULT 'Present',
            ""IsManualEntry"" BOOLEAN DEFAULT FALSE,
            ""Notes"" TEXT,
            ""ApprovedBy"" UUID,
            ""ApprovedAtUtc"" TIMESTAMP,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP,
            UNIQUE(""EmployeeId"", ""Date"")
        );

        -- Attendance Adjustments/Corrections
        CREATE TABLE IF NOT EXISTS ""AttendanceAdjustments"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""AttendanceRecordId"" UUID NOT NULL REFERENCES ""AttendanceRecords""(""Id"") ON DELETE CASCADE,
            ""OriginalCheckIn"" TIME,
            ""OriginalCheckOut"" TIME,
            ""AdjustedCheckIn"" TIME,
            ""AdjustedCheckOut"" TIME,
            ""Reason"" TEXT NOT NULL,
            ""RequestedBy"" UUID REFERENCES ""Users""(""Id""),
            ""ApprovedBy"" UUID REFERENCES ""Users""(""Id""),
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Overtime Requests
        CREATE TABLE IF NOT EXISTS ""OvertimeRequests"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Date"" DATE NOT NULL,
            ""StartTime"" TIME NOT NULL,
            ""EndTime"" TIME NOT NULL,
            ""Hours"" DECIMAL(4,2) NOT NULL,
            ""Reason"" TEXT,
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""ApprovedBy"" UUID REFERENCES ""Users""(""Id""),
            ""ApprovedAtUtc"" TIMESTAMP,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );
    ";

    private string GetLeaveSchema() => @"
        -- Leave Types
        CREATE TABLE IF NOT EXISTS ""LeaveTypes"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Code"" VARCHAR(20) UNIQUE,
            ""Description"" TEXT,
            ""DefaultDays"" DECIMAL(5,2) DEFAULT 0,
            ""MaxDays"" DECIMAL(5,2),
            ""IsPaid"" BOOLEAN DEFAULT TRUE,
            ""IsCarryForward"" BOOLEAN DEFAULT FALSE,
            ""MaxCarryForward"" DECIMAL(5,2) DEFAULT 0,
            ""RequiresApproval"" BOOLEAN DEFAULT TRUE,
            ""RequiresDocument"" BOOLEAN DEFAULT FALSE,
            ""MinNoticeDays"" INTEGER DEFAULT 0,
            ""AllowHalfDay"" BOOLEAN DEFAULT TRUE,
            ""AllowNegativeBalance"" BOOLEAN DEFAULT FALSE,
            ""Color"" VARCHAR(7),
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Leave Policies
        CREATE TABLE IF NOT EXISTS ""LeavePolicies"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
            ""EmploymentType"" VARCHAR(50),
            ""MinServiceMonths"" INTEGER DEFAULT 0,
            ""AnnualAllowance"" DECIMAL(5,2) NOT NULL,
            ""AccrualType"" VARCHAR(20) DEFAULT 'Yearly',
            ""AccrualDay"" INTEGER,
            ""ProRataEnabled"" BOOLEAN DEFAULT TRUE,
            ""GenderSpecific"" VARCHAR(20),
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Leave Balances
        CREATE TABLE IF NOT EXISTS ""LeaveBalances"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
            ""Year"" INTEGER NOT NULL,
            ""Entitled"" DECIMAL(5,2) DEFAULT 0,
            ""CarriedForward"" DECIMAL(5,2) DEFAULT 0,
            ""Adjustment"" DECIMAL(5,2) DEFAULT 0,
            ""Used"" DECIMAL(5,2) DEFAULT 0,
            ""Pending"" DECIMAL(5,2) DEFAULT 0,
            ""Balance"" DECIMAL(5,2) DEFAULT 0,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP,
            UNIQUE(""EmployeeId"", ""LeaveTypeId"", ""Year"")
        );

        -- Leave Requests
        CREATE TABLE IF NOT EXISTS ""LeaveRequests"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
            ""StartDate"" DATE NOT NULL,
            ""EndDate"" DATE NOT NULL,
            ""StartHalf"" VARCHAR(10),
            ""EndHalf"" VARCHAR(10),
            ""TotalDays"" DECIMAL(5,2) NOT NULL,
            ""Reason"" TEXT,
            ""ContactDuringLeave"" VARCHAR(100),
            ""HandoverTo"" UUID REFERENCES ""Employees""(""Id""),
            ""HandoverNotes"" TEXT,
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""CancellationReason"" TEXT,
            ""DocumentUrl"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Leave Approvals
        CREATE TABLE IF NOT EXISTS ""LeaveApprovals"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""LeaveRequestId"" UUID NOT NULL REFERENCES ""LeaveRequests""(""Id"") ON DELETE CASCADE,
            ""ApproverId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""Level"" INTEGER DEFAULT 1,
            ""Status"" VARCHAR(20) NOT NULL,
            ""Comments"" TEXT,
            ""ActionAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Leave Accrual History
        CREATE TABLE IF NOT EXISTS ""LeaveAccruals"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""LeaveTypeId"" UUID NOT NULL REFERENCES ""LeaveTypes""(""Id""),
            ""AccrualDate"" DATE NOT NULL,
            ""Days"" DECIMAL(5,2) NOT NULL,
            ""Type"" VARCHAR(20) NOT NULL,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Insert default leave types
        INSERT INTO ""LeaveTypes"" (""Name"", ""Code"", ""DefaultDays"", ""IsPaid"", ""Color"") VALUES 
            ('Annual Leave', 'AL', 20, TRUE, '#4CAF50'),
            ('Sick Leave', 'SL', 10, TRUE, '#F44336'),
            ('Casual Leave', 'CL', 5, TRUE, '#2196F3'),
            ('Maternity Leave', 'ML', 90, TRUE, '#E91E63'),
            ('Paternity Leave', 'PL', 5, TRUE, '#9C27B0'),
            ('Unpaid Leave', 'UL', 0, FALSE, '#9E9E9E'),
            ('Compensatory Off', 'CO', 0, TRUE, '#FF9800'),
            ('Work From Home', 'WFH', 0, TRUE, '#00BCD4')
        ON CONFLICT (""Code"") DO NOTHING;
    ";

    private string GetPerformanceSchema() => @"
        -- Performance Review Cycles
        CREATE TABLE IF NOT EXISTS ""ReviewCycles"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""StartDate"" DATE NOT NULL,
            ""EndDate"" DATE NOT NULL,
            ""ReviewStartDate"" DATE,
            ""ReviewEndDate"" DATE,
            ""Status"" VARCHAR(20) DEFAULT 'Draft',
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Performance Reviews
        CREATE TABLE IF NOT EXISTS ""PerformanceReviews"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""ReviewCycleId"" UUID REFERENCES ""ReviewCycles""(""Id""),
            ""ReviewerId"" UUID REFERENCES ""Users""(""Id""),
            ""ReviewType"" VARCHAR(50) DEFAULT 'Annual',
            ""ReviewDate"" DATE,
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""OverallRating"" DECIMAL(3,2),
            ""SelfRating"" DECIMAL(3,2),
            ""ManagerRating"" DECIMAL(3,2),
            ""SelfComments"" TEXT,
            ""ManagerComments"" TEXT,
            ""Strengths"" TEXT,
            ""AreasForImprovement"" TEXT,
            ""Goals"" TEXT,
            ""EmployeeSignedAt"" TIMESTAMP,
            ""ManagerSignedAt"" TIMESTAMP,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Goals/Objectives
        CREATE TABLE IF NOT EXISTS ""Goals"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""ReviewCycleId"" UUID REFERENCES ""ReviewCycles""(""Id""),
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""Category"" VARCHAR(50),
            ""Priority"" VARCHAR(20) DEFAULT 'Medium',
            ""Weight"" DECIMAL(5,2),
            ""TargetValue"" DECIMAL(18,2),
            ""ActualValue"" DECIMAL(18,2),
            ""Unit"" VARCHAR(50),
            ""StartDate"" DATE,
            ""DueDate"" DATE,
            ""CompletedDate"" DATE,
            ""Progress"" INTEGER DEFAULT 0,
            ""Status"" VARCHAR(20) DEFAULT 'NotStarted',
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Competencies
        CREATE TABLE IF NOT EXISTS ""Competencies"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""Category"" VARCHAR(50),
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Performance Competency Ratings
        CREATE TABLE IF NOT EXISTS ""PerformanceCompetencies"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""PerformanceReviewId"" UUID NOT NULL REFERENCES ""PerformanceReviews""(""Id"") ON DELETE CASCADE,
            ""CompetencyId"" UUID NOT NULL REFERENCES ""Competencies""(""Id""),
            ""SelfRating"" INTEGER,
            ""ManagerRating"" INTEGER,
            ""Comments"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- 360 Feedback
        CREATE TABLE IF NOT EXISTS ""FeedbackRequests"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""ReviewCycleId"" UUID REFERENCES ""ReviewCycles""(""Id""),
            ""RequestedFrom"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""RelationshipType"" VARCHAR(50),
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""DueDate"" DATE,
            ""Feedback"" TEXT,
            ""Rating"" INTEGER,
            ""SubmittedAtUtc"" TIMESTAMP,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Recognition/Kudos
        CREATE TABLE IF NOT EXISTS ""Recognitions"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""FromUserId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""ToEmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Category"" VARCHAR(50),
            ""Title"" VARCHAR(200),
            ""Message"" TEXT NOT NULL,
            ""Points"" INTEGER DEFAULT 0,
            ""IsPublic"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ";

    private string GetRecruitingSchema() => @"
        -- Legacy Job Postings (keeping for backward compatibility)
        CREATE TABLE IF NOT EXISTS ""JobPostings"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Title"" VARCHAR(200) NOT NULL,
            ""JobCode"" VARCHAR(50),
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""Location"" VARCHAR(200),
            ""EmploymentType"" VARCHAR(50) DEFAULT 'FullTime',
            ""ExperienceLevel"" VARCHAR(50),
            ""Description"" TEXT,
            ""Requirements"" TEXT,
            ""Responsibilities"" TEXT,
            ""Benefits"" TEXT,
            ""SalaryMin"" DECIMAL(18,2),
            ""SalaryMax"" DECIMAL(18,2),
            ""Currency"" VARCHAR(3) DEFAULT 'USD',
            ""ShowSalary"" BOOLEAN DEFAULT FALSE,
            ""Openings"" INTEGER DEFAULT 1,
            ""Status"" INTEGER DEFAULT 0,
            ""PublishedAt"" TIMESTAMP,
            ""ClosingDate"" DATE,
            ""HiringManagerId"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Legacy Applicants (keeping for backward compatibility)
        CREATE TABLE IF NOT EXISTS ""Applicants"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""JobPostingId"" UUID NOT NULL REFERENCES ""JobPostings""(""Id"") ON DELETE CASCADE,
            ""FirstName"" VARCHAR(100) NOT NULL,
            ""LastName"" VARCHAR(100) NOT NULL,
            ""Email"" VARCHAR(255) NOT NULL,
            ""Phone"" VARCHAR(20),
            ""ResumePath"" TEXT,
            ""CoverLetterPath"" TEXT,
            ""LinkedInUrl"" VARCHAR(255),
            ""PortfolioUrl"" VARCHAR(255),
            ""Source"" VARCHAR(50),
            ""ReferredBy"" VARCHAR(200),
            ""Status"" INTEGER DEFAULT 0,
            ""Stage"" INTEGER DEFAULT 0,
            ""Rating"" INTEGER,
            ""Notes"" TEXT,
            ""ExpectedSalary"" DECIMAL(18,2),
            ""NoticePeriod"" VARCHAR(50),
            ""AppliedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Job Requisitions
        CREATE TABLE IF NOT EXISTS ""JobRequisitions"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""RequisitionNumber"" VARCHAR(50) NOT NULL UNIQUE,
            ""Title"" VARCHAR(200) NOT NULL,
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
            ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
            ""Location"" VARCHAR(200),
            ""EmploymentType"" VARCHAR(50) DEFAULT 'FullTime',
            ""Openings"" INTEGER DEFAULT 1,
            ""BudgetMin"" DECIMAL(18,2),
            ""BudgetMax"" DECIMAL(18,2),
            ""Currency"" VARCHAR(3) DEFAULT 'USD',
            ""Description"" TEXT,
            ""Requirements"" TEXT,
            ""Responsibilities"" TEXT,
            ""RequiredSkills"" TEXT,
            ""PreferredSkills"" TEXT,
            ""MinExperienceYears"" INTEGER,
            ""MaxExperienceYears"" INTEGER,
            ""EducationLevel"" VARCHAR(100),
            ""Status"" INTEGER DEFAULT 0,
            ""RequestedById"" UUID REFERENCES ""Users""(""Id""),
            ""HiringManagerId"" UUID REFERENCES ""Employees""(""Id""),
            ""ApprovedAt"" TIMESTAMP,
            ""PublishedAt"" TIMESTAMP,
            ""ClosedAt"" TIMESTAMP,
            ""TargetStartDate"" DATE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP,
            ""IsDeleted"" BOOLEAN DEFAULT FALSE
        );

        -- Job Requisition Approvals
        CREATE TABLE IF NOT EXISTS ""JobRequisitionApprovals"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""RequisitionId"" UUID NOT NULL REFERENCES ""JobRequisitions""(""Id"") ON DELETE CASCADE,
            ""ApproverId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""ApprovalLevel"" INTEGER NOT NULL,
            ""Status"" INTEGER DEFAULT 0,
            ""Comments"" TEXT,
            ""ApprovedAt"" TIMESTAMP,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Candidates
        CREATE TABLE IF NOT EXISTS ""Candidates"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""FirstName"" VARCHAR(100) NOT NULL,
            ""LastName"" VARCHAR(100) NOT NULL,
            ""Email"" VARCHAR(255) NOT NULL,
            ""Phone"" VARCHAR(20),
            ""AlternatePhone"" VARCHAR(20),
            ""DateOfBirth"" DATE,
            ""Gender"" VARCHAR(20),
            ""Address"" TEXT,
            ""City"" VARCHAR(100),
            ""State"" VARCHAR(100),
            ""Country"" VARCHAR(100),
            ""PostalCode"" VARCHAR(20),
            ""LinkedInUrl"" VARCHAR(255),
            ""PortfolioUrl"" VARCHAR(255),
            ""Website"" VARCHAR(255),
            ""CurrentCompany"" VARCHAR(200),
            ""CurrentTitle"" VARCHAR(100),
            ""CurrentSalary"" DECIMAL(18,2),
            ""NoticePeriod"" VARCHAR(50),
            ""ExpectedSalary"" DECIMAL(18,2),
            ""Source"" VARCHAR(50),
            ""ReferredByEmployeeId"" UUID REFERENCES ""Employees""(""Id""),
            ""ReferralCode"" VARCHAR(50),
            ""Tags"" TEXT,
            ""Notes"" TEXT,
            ""Rating"" INTEGER,
            ""EmailHash"" VARCHAR(255),
            ""PhoneHash"" VARCHAR(255),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP,
            ""IsDeleted"" BOOLEAN DEFAULT FALSE
        );

        -- Applications
        CREATE TABLE IF NOT EXISTS ""Applications"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""CandidateId"" UUID NOT NULL REFERENCES ""Candidates""(""Id"") ON DELETE CASCADE,
            ""RequisitionId"" UUID NOT NULL REFERENCES ""JobRequisitions""(""Id""),
            ""Stage"" INTEGER DEFAULT 0,
            ""Status"" INTEGER DEFAULT 0,
            ""CoverLetter"" TEXT,
            ""ScreeningAnswers"" TEXT,
            ""AppliedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""ShortlistedAt"" TIMESTAMP,
            ""RejectedAt"" TIMESTAMP,
            ""RejectionReason"" TEXT,
            ""Source"" VARCHAR(50),
            ""ReferralCode"" VARCHAR(50),
            ""UtmSource"" VARCHAR(100),
            ""UtmMedium"" VARCHAR(100),
            ""UtmCampaign"" VARCHAR(100),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Candidate Documents
        CREATE TABLE IF NOT EXISTS ""CandidateDocuments"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""CandidateId"" UUID NOT NULL REFERENCES ""Candidates""(""Id"") ON DELETE CASCADE,
            ""DocumentType"" VARCHAR(50) NOT NULL,
            ""FileName"" VARCHAR(200) NOT NULL,
            ""FilePath"" TEXT NOT NULL,
            ""FileSize"" BIGINT NOT NULL,
            ""MimeType"" VARCHAR(100),
            ""IsPrimary"" BOOLEAN DEFAULT FALSE,
            ""UploadedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UploadedById"" UUID REFERENCES ""Users""(""Id"")
        );

        -- Application Activities
        CREATE TABLE IF NOT EXISTS ""ApplicationActivities"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""ApplicationId"" UUID NOT NULL REFERENCES ""Applications""(""Id"") ON DELETE CASCADE,
            ""Type"" INTEGER NOT NULL,
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""PerformedById"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Interviews (updated to link to Applications)
        CREATE TABLE IF NOT EXISTS ""Interviews"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""ApplicationId"" UUID NOT NULL REFERENCES ""Applications""(""Id"") ON DELETE CASCADE,
            ""InterviewType"" VARCHAR(50) NOT NULL,
            ""InterviewRound"" VARCHAR(50),
            ""ScheduledAt"" TIMESTAMP NOT NULL,
            ""CompletedAt"" TIMESTAMP,
            ""DurationMinutes"" INTEGER DEFAULT 60,
            ""Location"" VARCHAR(200),
            ""MeetingLink"" TEXT,
            ""MeetingId"" VARCHAR(100),
            ""MeetingPassword"" VARCHAR(100),
            ""Status"" INTEGER DEFAULT 0,
            ""InterviewerIds"" TEXT,
            ""PanelMembers"" TEXT,
            ""Agenda"" TEXT,
            ""Notes"" TEXT,
            ""OverallRating"" INTEGER,
            ""Feedback"" TEXT,
            ""Strengths"" TEXT,
            ""Weaknesses"" TEXT,
            ""Recommendation"" VARCHAR(50),
            ""ScheduledById"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Interview Feedback
        CREATE TABLE IF NOT EXISTS ""InterviewFeedbacks"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""InterviewId"" UUID NOT NULL REFERENCES ""Interviews""(""Id"") ON DELETE CASCADE,
            ""InterviewerId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""OverallRating"" INTEGER,
            ""TechnicalScore"" TEXT,
            ""CommunicationScore"" TEXT,
            ""CulturalFitScore"" TEXT,
            ""ProblemSolvingScore"" TEXT,
            ""Strengths"" TEXT,
            ""Weaknesses"" TEXT,
            ""OverallComments"" TEXT,
            ""Recommendation"" INTEGER DEFAULT 0,
            ""RecommendationNotes"" TEXT,
            ""SubmittedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Assessments
        CREATE TABLE IF NOT EXISTS ""Assessments"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""CandidateId"" UUID NOT NULL REFERENCES ""Candidates""(""Id"") ON DELETE CASCADE,
            ""ApplicationId"" UUID REFERENCES ""Applications""(""Id""),
            ""AssessmentName"" VARCHAR(200) NOT NULL,
            ""AssessmentType"" VARCHAR(50) NOT NULL,
            ""Instructions"" TEXT,
            ""Questions"" TEXT,
            ""Answers"" TEXT,
            ""Score"" DECIMAL(10,2),
            ""MaxScore"" DECIMAL(10,2),
            ""PassingScore"" DECIMAL(10,2),
            ""Status"" INTEGER DEFAULT 0,
            ""IsPassed"" BOOLEAN DEFAULT FALSE,
            ""AssignedAt"" TIMESTAMP,
            ""StartedAt"" TIMESTAMP,
            ""CompletedAt"" TIMESTAMP,
            ""DueDate"" DATE,
            ""Feedback"" TEXT,
            ""Attachments"" TEXT,
            ""AssignedById"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Offers
        CREATE TABLE IF NOT EXISTS ""Offers"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""ApplicationId"" UUID NOT NULL REFERENCES ""Applications""(""Id"") ON DELETE CASCADE,
            ""RequisitionId"" UUID REFERENCES ""JobRequisitions""(""Id""),
            ""OfferNumber"" VARCHAR(50) NOT NULL UNIQUE,
            ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
            ""BaseSalary"" DECIMAL(18,2) NOT NULL,
            ""Currency"" VARCHAR(3) DEFAULT 'USD',
            ""SalaryBreakdown"" TEXT,
            ""Benefits"" TEXT,
            ""JoiningDate"" DATE,
            ""OfferDate"" TIMESTAMP,
            ""ExpiryDate"" DATE,
            ""Status"" INTEGER DEFAULT 0,
            ""OfferLetterTemplate"" VARCHAR(100),
            ""OfferLetterContent"" TEXT,
            ""OfferDocumentPath"" TEXT,
            ""SignedDocumentPath"" TEXT,
            ""ApprovedById"" UUID REFERENCES ""Users""(""Id""),
            ""ApprovedAt"" TIMESTAMP,
            ""SentAt"" TIMESTAMP,
            ""AcceptedAt"" TIMESTAMP,
            ""RejectedAt"" TIMESTAMP,
            ""RejectionReason"" TEXT,
            ""AcceptanceNotes"" TEXT,
            ""ConvertedToEmployeeId"" UUID REFERENCES ""Employees""(""Id""),
            ""ConvertedAt"" TIMESTAMP,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Email Logs
        CREATE TABLE IF NOT EXISTS ""EmailLogs"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""ToEmail"" VARCHAR(255) NOT NULL,
            ""ToName"" VARCHAR(200),
            ""Subject"" VARCHAR(500) NOT NULL,
            ""Body"" TEXT NOT NULL,
            ""EmailType"" VARCHAR(50) NOT NULL,
            ""RelatedEntityId"" UUID,
            ""RelatedEntityType"" VARCHAR(50),
            ""Status"" INTEGER DEFAULT 0,
            ""ErrorMessage"" TEXT,
            ""SentAt"" TIMESTAMP,
            ""DeliveredAt"" TIMESTAMP,
            ""OpenedAt"" TIMESTAMP,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Indexes
        CREATE INDEX IF NOT EXISTS idx_candidates_email ON ""Candidates""(""Email"");
        CREATE INDEX IF NOT EXISTS idx_applications_requisition ON ""Applications""(""RequisitionId"");
        CREATE INDEX IF NOT EXISTS idx_applications_candidate ON ""Applications""(""CandidateId"");
        CREATE INDEX IF NOT EXISTS idx_applications_stage ON ""Applications""(""Stage"");
        CREATE INDEX IF NOT EXISTS idx_interviews_application ON ""Interviews""(""ApplicationId"");
        CREATE INDEX IF NOT EXISTS idx_interviews_scheduled ON ""Interviews""(""ScheduledAt"");
        CREATE INDEX IF NOT EXISTS idx_offers_application ON ""Offers""(""ApplicationId"");
        CREATE INDEX IF NOT EXISTS idx_email_logs_entity ON ""EmailLogs""(""RelatedEntityId"", ""RelatedEntityType"");
    ";

    private string GetBenefitsSchema() => @"
        -- Benefit Plans
        CREATE TABLE IF NOT EXISTS ""BenefitPlans"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""Type"" VARCHAR(50) NOT NULL,
            ""Provider"" VARCHAR(200),
            ""PlanNumber"" VARCHAR(50),
            ""CoverageType"" VARCHAR(50),
            ""EmployeeContribution"" DECIMAL(10,2) DEFAULT 0,
            ""EmployerContribution"" DECIMAL(10,2) DEFAULT 0,
            ""ContributionFrequency"" VARCHAR(20) DEFAULT 'Monthly',
            ""EligibilityWaitDays"" INTEGER DEFAULT 0,
            ""StartDate"" DATE,
            ""EndDate"" DATE,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Benefits Enrollment
        CREATE TABLE IF NOT EXISTS ""EmployeeBenefits"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""BenefitPlanId"" UUID NOT NULL REFERENCES ""BenefitPlans""(""Id""),
            ""EnrollmentDate"" DATE NOT NULL,
            ""EffectiveDate"" DATE NOT NULL,
            ""TerminationDate"" DATE,
            ""CoverageLevel"" VARCHAR(50),
            ""EmployeeContribution"" DECIMAL(10,2),
            ""EmployerContribution"" DECIMAL(10,2),
            ""Status"" VARCHAR(20) DEFAULT 'Active',
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Benefit Dependents
        CREATE TABLE IF NOT EXISTS ""BenefitDependents"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeBenefitId"" UUID NOT NULL REFERENCES ""EmployeeBenefits""(""Id"") ON DELETE CASCADE,
            ""DependentId"" UUID REFERENCES ""EmployeeDependents""(""Id""),
            ""Name"" VARCHAR(200),
            ""Relationship"" VARCHAR(50),
            ""DateOfBirth"" DATE,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Open Enrollment Periods
        CREATE TABLE IF NOT EXISTS ""OpenEnrollments"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Year"" INTEGER NOT NULL,
            ""StartDate"" DATE NOT NULL,
            ""EndDate"" DATE NOT NULL,
            ""EffectiveDate"" DATE NOT NULL,
            ""Status"" VARCHAR(20) DEFAULT 'Upcoming',
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );
    ";

    private string GetTrainingSchema() => @"
        -- Training Programs/Courses
        CREATE TABLE IF NOT EXISTS ""TrainingPrograms"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""Category"" VARCHAR(100),
            ""Type"" VARCHAR(50) DEFAULT 'Online',
            ""Provider"" VARCHAR(200),
            ""Duration"" INTEGER,
            ""DurationUnit"" VARCHAR(20) DEFAULT 'Hours',
            ""Cost"" DECIMAL(10,2) DEFAULT 0,
            ""Currency"" VARCHAR(3) DEFAULT 'USD',
            ""MaxParticipants"" INTEGER,
            ""IsMandatory"" BOOLEAN DEFAULT FALSE,
            ""CertificationProvided"" BOOLEAN DEFAULT FALSE,
            ""ExternalUrl"" TEXT,
            ""Materials"" TEXT,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Training Sessions
        CREATE TABLE IF NOT EXISTS ""TrainingSessions"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""TrainingProgramId"" UUID NOT NULL REFERENCES ""TrainingPrograms""(""Id"") ON DELETE CASCADE,
            ""Title"" VARCHAR(200),
            ""TrainerId"" UUID REFERENCES ""Users""(""Id""),
            ""ExternalTrainer"" VARCHAR(200),
            ""StartDate"" TIMESTAMP NOT NULL,
            ""EndDate"" TIMESTAMP NOT NULL,
            ""Location"" VARCHAR(200),
            ""MeetingLink"" TEXT,
            ""MaxParticipants"" INTEGER,
            ""Status"" VARCHAR(20) DEFAULT 'Scheduled',
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Training Assignments
        CREATE TABLE IF NOT EXISTS ""EmployeeTrainings"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""TrainingProgramId"" UUID NOT NULL REFERENCES ""TrainingPrograms""(""Id""),
            ""TrainingSessionId"" UUID REFERENCES ""TrainingSessions""(""Id""),
            ""AssignedBy"" UUID REFERENCES ""Users""(""Id""),
            ""AssignedDate"" DATE NOT NULL,
            ""DueDate"" DATE,
            ""StartedDate"" DATE,
            ""CompletedDate"" DATE,
            ""Progress"" INTEGER DEFAULT 0,
            ""Score"" DECIMAL(5,2),
            ""Status"" VARCHAR(20) DEFAULT 'Assigned',
            ""CertificateUrl"" TEXT,
            ""Feedback"" TEXT,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Certifications
        CREATE TABLE IF NOT EXISTS ""EmployeeCertifications"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""Name"" VARCHAR(200) NOT NULL,
            ""IssuingOrganization"" VARCHAR(200),
            ""CredentialId"" VARCHAR(100),
            ""CredentialUrl"" TEXT,
            ""IssueDate"" DATE,
            ""ExpiryDate"" DATE,
            ""Status"" VARCHAR(20) DEFAULT 'Active',
            ""DocumentUrl"" TEXT,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Skills
        CREATE TABLE IF NOT EXISTS ""Skills"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL UNIQUE,
            ""Category"" VARCHAR(50),
            ""Description"" TEXT,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Skills
        CREATE TABLE IF NOT EXISTS ""EmployeeSkills"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""SkillId"" UUID NOT NULL REFERENCES ""Skills""(""Id""),
            ""ProficiencyLevel"" VARCHAR(20),
            ""YearsOfExperience"" INTEGER,
            ""LastUsedDate"" DATE,
            ""IsPrimary"" BOOLEAN DEFAULT FALSE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            UNIQUE(""EmployeeId"", ""SkillId"")
        );
    ";

    private string GetOnboardingSchema() => @"
        -- Onboarding Templates
        CREATE TABLE IF NOT EXISTS ""OnboardingTemplates"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""DesignationId"" UUID REFERENCES ""Designations""(""Id""),
            ""DurationDays"" INTEGER DEFAULT 30,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Onboarding Template Tasks
        CREATE TABLE IF NOT EXISTS ""OnboardingTemplateTasks"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""TemplateId"" UUID NOT NULL REFERENCES ""OnboardingTemplates""(""Id"") ON DELETE CASCADE,
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""Category"" VARCHAR(50),
            ""AssigneeType"" VARCHAR(50) DEFAULT 'Employee',
            ""DueDayOffset"" INTEGER DEFAULT 0,
            ""IsMandatory"" BOOLEAN DEFAULT TRUE,
            ""SortOrder"" INTEGER DEFAULT 0,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Employee Onboarding
        CREATE TABLE IF NOT EXISTS ""EmployeeOnboarding"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""TemplateId"" UUID REFERENCES ""OnboardingTemplates""(""Id""),
            ""StartDate"" DATE NOT NULL,
            ""ExpectedEndDate"" DATE,
            ""ActualEndDate"" DATE,
            ""Status"" VARCHAR(20) DEFAULT 'InProgress',
            ""Progress"" INTEGER DEFAULT 0,
            ""BuddyId"" UUID REFERENCES ""Employees""(""Id""),
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Onboarding Tasks
        CREATE TABLE IF NOT EXISTS ""OnboardingTasks"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""OnboardingId"" UUID NOT NULL REFERENCES ""EmployeeOnboarding""(""Id"") ON DELETE CASCADE,
            ""TemplateTaksId"" UUID REFERENCES ""OnboardingTemplateTasks""(""Id""),
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""Category"" VARCHAR(50),
            ""AssignedTo"" UUID REFERENCES ""Users""(""Id""),
            ""DueDate"" DATE,
            ""CompletedDate"" DATE,
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Offboarding/Exit
        CREATE TABLE IF NOT EXISTS ""EmployeeOffboarding"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""LastWorkingDate"" DATE NOT NULL,
            ""ExitType"" VARCHAR(50) NOT NULL,
            ""Reason"" TEXT,
            ""IsRehirable"" BOOLEAN,
            ""ExitInterviewDate"" DATE,
            ""ExitInterviewNotes"" TEXT,
            ""Status"" VARCHAR(20) DEFAULT 'Initiated',
            ""ClearanceStatus"" VARCHAR(20) DEFAULT 'Pending',
            ""SettlementStatus"" VARCHAR(20) DEFAULT 'Pending',
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Exit Interview Questions
        CREATE TABLE IF NOT EXISTS ""ExitInterviewResponses"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""OffboardingId"" UUID NOT NULL REFERENCES ""EmployeeOffboarding""(""Id"") ON DELETE CASCADE,
            ""Question"" TEXT NOT NULL,
            ""Response"" TEXT,
            ""Category"" VARCHAR(50),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Clearance Items
        CREATE TABLE IF NOT EXISTS ""ClearanceItems"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""OffboardingId"" UUID NOT NULL REFERENCES ""EmployeeOffboarding""(""Id"") ON DELETE CASCADE,
            ""Department"" VARCHAR(100) NOT NULL,
            ""Item"" VARCHAR(200) NOT NULL,
            ""AssignedTo"" UUID REFERENCES ""Users""(""Id""),
            ""Status"" VARCHAR(20) DEFAULT 'Pending',
            ""CompletedDate"" DATE,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );
    ";

    private string GetPayrollSchema() => @"
        -- Payroll Periods
        CREATE TABLE IF NOT EXISTS ""PayrollPeriods"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""StartDate"" DATE NOT NULL,
            ""EndDate"" DATE NOT NULL,
            ""PayDate"" DATE NOT NULL,
            ""Status"" VARCHAR(20) DEFAULT 'Open',
            ""Notes"" TEXT,
            ""ProcessedAt"" TIMESTAMP,
            ""ProcessedBy"" UUID,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Salary Components
        CREATE TABLE IF NOT EXISTS ""SalaryComponents"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Code"" VARCHAR(20) UNIQUE,
            ""Type"" VARCHAR(20) NOT NULL,
            ""CalculationType"" VARCHAR(20) DEFAULT 'Fixed',
            ""TaxablePercentage"" DECIMAL(5,2) DEFAULT 100,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Employee Salary Structure
        CREATE TABLE IF NOT EXISTS ""EmployeeSalaryStructure"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""SalaryComponentId"" UUID NOT NULL REFERENCES ""SalaryComponents""(""Id""),
            ""Amount"" DECIMAL(18,2) NOT NULL,
            ""EffectiveDate"" DATE NOT NULL,
            ""EndDate"" DATE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Payroll Runs
        CREATE TABLE IF NOT EXISTS ""PayrollRuns"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""PayrollPeriodId"" UUID NOT NULL REFERENCES ""PayrollPeriods""(""Id""),
            ""EmployeeId"" UUID NOT NULL REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""GrossSalary"" DECIMAL(18,2) NOT NULL,
            ""TotalDeductions"" DECIMAL(18,2) DEFAULT 0,
            ""TotalAdditions"" DECIMAL(18,2) DEFAULT 0,
            ""NetSalary"" DECIMAL(18,2) NOT NULL,
            ""WorkingDays"" DECIMAL(5,2),
            ""PresentDays"" DECIMAL(5,2),
            ""LeaveDays"" DECIMAL(5,2),
            ""OvertimeHours"" DECIMAL(6,2),
            ""Status"" VARCHAR(20) DEFAULT 'Calculated',
            ""PaidAt"" TIMESTAMP,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Payroll Details (line items)
        CREATE TABLE IF NOT EXISTS ""PayrollDetails"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""PayrollRunId"" UUID NOT NULL REFERENCES ""PayrollRuns""(""Id"") ON DELETE CASCADE,
            ""SalaryComponentId"" UUID NOT NULL REFERENCES ""SalaryComponents""(""Id""),
            ""Amount"" DECIMAL(18,2) NOT NULL,
            ""Type"" VARCHAR(20) NOT NULL,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Tax Configurations
        CREATE TABLE IF NOT EXISTS ""TaxConfigurations"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Country"" VARCHAR(100),
            ""TaxYear"" INTEGER NOT NULL,
            ""MinIncome"" DECIMAL(18,2),
            ""MaxIncome"" DECIMAL(18,2),
            ""Rate"" DECIMAL(5,2) NOT NULL,
            ""FixedAmount"" DECIMAL(18,2) DEFAULT 0,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Insert default salary components
        INSERT INTO ""SalaryComponents"" (""Name"", ""Code"", ""Type"", ""TaxablePercentage"") VALUES 
            ('Basic Salary', 'BASIC', 'Earning', 100),
            ('House Rent Allowance', 'HRA', 'Earning', 50),
            ('Transport Allowance', 'TA', 'Earning', 100),
            ('Medical Allowance', 'MA', 'Earning', 0),
            ('Performance Bonus', 'BONUS', 'Earning', 100),
            ('Overtime', 'OT', 'Earning', 100),
            ('Tax Deduction', 'TAX', 'Deduction', 0),
            ('Insurance Premium', 'INS', 'Deduction', 0),
            ('Loan Repayment', 'LOAN', 'Deduction', 0),
            ('Provident Fund', 'PF', 'Deduction', 0)
        ON CONFLICT (""Code"") DO NOTHING;
    ";

    private string GetDocumentSchema() => @"
        -- Document Categories
        CREATE TABLE IF NOT EXISTS ""DocumentCategories"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(100) NOT NULL,
            ""Description"" TEXT,
            ""ParentId"" UUID REFERENCES ""DocumentCategories""(""Id""),
            ""IsSystem"" BOOLEAN DEFAULT FALSE,
            ""IsActive"" BOOLEAN DEFAULT TRUE,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Documents
        CREATE TABLE IF NOT EXISTS ""Documents"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""CategoryId"" UUID REFERENCES ""DocumentCategories""(""Id""),
            ""EmployeeId"" UUID REFERENCES ""Employees""(""Id"") ON DELETE CASCADE,
            ""FileUrl"" TEXT NOT NULL,
            ""FileName"" VARCHAR(200) NOT NULL,
            ""FileType"" VARCHAR(50),
            ""FileSize"" BIGINT,
            ""Version"" INTEGER DEFAULT 1,
            ""IsConfidential"" BOOLEAN DEFAULT FALSE,
            ""ExpiryDate"" DATE,
            ""Tags"" TEXT,
            ""UploadedBy"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Document Access Log
        CREATE TABLE IF NOT EXISTS ""DocumentAccessLogs"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""DocumentId"" UUID NOT NULL REFERENCES ""Documents""(""Id"") ON DELETE CASCADE,
            ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""Action"" VARCHAR(50) NOT NULL,
            ""IPAddress"" VARCHAR(50),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Insert default document categories
        INSERT INTO ""DocumentCategories"" (""Name"", ""IsSystem"") VALUES 
            ('Personal Documents', TRUE),
            ('Employment Documents', TRUE),
            ('Policies', TRUE),
            ('Training Materials', TRUE),
            ('Templates', TRUE)
        ON CONFLICT DO NOTHING;
    ";

    private string GetAnnouncementsSchema() => @"
        -- Announcements
        CREATE TABLE IF NOT EXISTS ""Announcements"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Title"" VARCHAR(200) NOT NULL,
            ""Content"" TEXT NOT NULL,
            ""Type"" VARCHAR(50) DEFAULT 'General',
            ""Priority"" VARCHAR(20) DEFAULT 'Normal',
            ""PublishDate"" TIMESTAMP NOT NULL,
            ""ExpiryDate"" TIMESTAMP,
            ""TargetAudience"" VARCHAR(50) DEFAULT 'All',
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
            ""AttachmentUrl"" TEXT,
            ""IsPinned"" BOOLEAN DEFAULT FALSE,
            ""RequiresAcknowledgement"" BOOLEAN DEFAULT FALSE,
            ""Status"" VARCHAR(20) DEFAULT 'Draft',
            ""CreatedBy"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Announcement Acknowledgements
        CREATE TABLE IF NOT EXISTS ""AnnouncementAcknowledgements"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""AnnouncementId"" UUID NOT NULL REFERENCES ""Announcements""(""Id"") ON DELETE CASCADE,
            ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""AcknowledgedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            UNIQUE(""AnnouncementId"", ""UserId"")
        );

        -- Company Events
        CREATE TABLE IF NOT EXISTS ""CompanyEvents"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""StartDate"" TIMESTAMP NOT NULL,
            ""EndDate"" TIMESTAMP NOT NULL,
            ""Location"" VARCHAR(200),
            ""IsAllDay"" BOOLEAN DEFAULT FALSE,
            ""Type"" VARCHAR(50),
            ""DepartmentId"" UUID REFERENCES ""Departments""(""Id""),
            ""BranchId"" UUID REFERENCES ""Branches""(""Id""),
            ""IsPublic"" BOOLEAN DEFAULT TRUE,
            ""MaxAttendees"" INTEGER,
            ""CreatedBy"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Event Attendees
        CREATE TABLE IF NOT EXISTS ""EventAttendees"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""EventId"" UUID NOT NULL REFERENCES ""CompanyEvents""(""Id"") ON DELETE CASCADE,
            ""UserId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
            ""Status"" VARCHAR(20) DEFAULT 'Invited',
            ""ResponseDate"" TIMESTAMP,
            ""Notes"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            UNIQUE(""EventId"", ""UserId"")
        );

        -- Polls/Surveys
        CREATE TABLE IF NOT EXISTS ""Surveys"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Title"" VARCHAR(200) NOT NULL,
            ""Description"" TEXT,
            ""Type"" VARCHAR(50) DEFAULT 'Survey',
            ""StartDate"" DATE NOT NULL,
            ""EndDate"" DATE NOT NULL,
            ""IsAnonymous"" BOOLEAN DEFAULT FALSE,
            ""Status"" VARCHAR(20) DEFAULT 'Draft',
            ""CreatedBy"" UUID REFERENCES ""Users""(""Id""),
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAtUtc"" TIMESTAMP
        );

        -- Survey Questions
        CREATE TABLE IF NOT EXISTS ""SurveyQuestions"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""SurveyId"" UUID NOT NULL REFERENCES ""Surveys""(""Id"") ON DELETE CASCADE,
            ""Question"" TEXT NOT NULL,
            ""Type"" VARCHAR(50) DEFAULT 'Text',
            ""Options"" TEXT,
            ""IsRequired"" BOOLEAN DEFAULT TRUE,
            ""SortOrder"" INTEGER DEFAULT 0,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Survey Responses
        CREATE TABLE IF NOT EXISTS ""SurveyResponses"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""SurveyId"" UUID NOT NULL REFERENCES ""Surveys""(""Id"") ON DELETE CASCADE,
            ""QuestionId"" UUID NOT NULL REFERENCES ""SurveyQuestions""(""Id"") ON DELETE CASCADE,
            ""UserId"" UUID REFERENCES ""Users""(""Id""),
            ""Response"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );

        -- Audit Log
        CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""UserId"" UUID REFERENCES ""Users""(""Id""),
            ""Action"" VARCHAR(100) NOT NULL,
            ""EntityType"" VARCHAR(100),
            ""EntityId"" UUID,
            ""OldValues"" TEXT,
            ""NewValues"" TEXT,
            ""IPAddress"" VARCHAR(50),
            ""UserAgent"" TEXT,
            ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ";
}
