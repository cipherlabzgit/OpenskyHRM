using Npgsql;

namespace Tenant.Api.Middleware;

public static class SchemaMigrationHelper
{
    public static async Task EnsureRecruitmentTablesExistAsync(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if JobRequisitions table exists
            await using var checkCmd = new NpgsqlCommand(
                @"SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'JobRequisitions'
                );", connection);
            
            var tableExists = (bool)(await checkCmd.ExecuteScalarAsync(cancellationToken) ?? false);
            
            if (!tableExists)
            {
                logger.LogWarning("JobRequisitions table does not exist. Creating recruitment tables...");
                await CreateRecruitmentTablesAsync(connection, logger, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring recruitment tables exist");
            throw;
        }
    }

    private static async Task CreateRecruitmentTablesAsync(NpgsqlConnection connection, ILogger logger, CancellationToken cancellationToken)
    {
        var sql = @"
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
                ""RequisitionId"" UUID NOT NULL REFERENCES ""JobRequisitions""(""Id""),
                ""CandidateId"" UUID NOT NULL REFERENCES ""Candidates""(""Id""),
                ""ApplicationNumber"" VARCHAR(50) NOT NULL UNIQUE,
                ""Stage"" VARCHAR(50) DEFAULT 'Applied',
                ""Status"" INTEGER DEFAULT 0,
                ""AppliedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""Source"" VARCHAR(50),
                ""ResumePath"" TEXT,
                ""CoverLetter"" TEXT,
                ""Notes"" TEXT,
                ""Rating"" INTEGER,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""UpdatedAtUtc"" TIMESTAMP
            );

            -- Candidate Documents
            CREATE TABLE IF NOT EXISTS ""CandidateDocuments"" (
                ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                ""CandidateId"" UUID NOT NULL REFERENCES ""Candidates""(""Id"") ON DELETE CASCADE,
                ""DocumentType"" VARCHAR(50) NOT NULL,
                ""FileName"" VARCHAR(255) NOT NULL,
                ""FilePath"" TEXT NOT NULL,
                ""FileSize"" BIGINT,
                ""MimeType"" VARCHAR(100),
                ""UploadedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- Application Activities
            CREATE TABLE IF NOT EXISTS ""ApplicationActivities"" (
                ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                ""ApplicationId"" UUID NOT NULL REFERENCES ""Applications""(""Id"") ON DELETE CASCADE,
                ""ActivityType"" VARCHAR(50) NOT NULL,
                ""Description"" TEXT,
                ""PerformedById"" UUID REFERENCES ""Users""(""Id""),
                ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- Interviews
            CREATE TABLE IF NOT EXISTS ""Interviews"" (
                ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                ""ApplicationId"" UUID NOT NULL REFERENCES ""Applications""(""Id"") ON DELETE CASCADE,
                ""InterviewType"" VARCHAR(50) NOT NULL,
                ""ScheduledAt"" TIMESTAMP NOT NULL,
                ""DurationMinutes"" INTEGER,
                ""Location"" VARCHAR(200),
                ""InterviewerId"" UUID REFERENCES ""Users""(""Id""),
                ""Status"" INTEGER DEFAULT 0,
                ""Notes"" TEXT,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""UpdatedAtUtc"" TIMESTAMP
            );

            -- Interview Feedback
            CREATE TABLE IF NOT EXISTS ""InterviewFeedbacks"" (
                ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                ""InterviewId"" UUID NOT NULL REFERENCES ""Interviews""(""Id"") ON DELETE CASCADE,
                ""InterviewerId"" UUID NOT NULL REFERENCES ""Users""(""Id""),
                ""Rating"" INTEGER,
                ""Strengths"" TEXT,
                ""Weaknesses"" TEXT,
                ""Recommendation"" VARCHAR(50),
                ""Comments"" TEXT,
                ""SubmittedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- Assessments
            CREATE TABLE IF NOT EXISTS ""Assessments"" (
                ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                ""ApplicationId"" UUID NOT NULL REFERENCES ""Applications""(""Id"") ON DELETE CASCADE,
                ""AssessmentType"" VARCHAR(50) NOT NULL,
                ""Title"" VARCHAR(200) NOT NULL,
                ""Score"" DECIMAL(5,2),
                ""MaxScore"" DECIMAL(5,2),
                ""Result"" VARCHAR(50),
                ""CompletedAt"" TIMESTAMP,
                ""CreatedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW()
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
                ""ApplicationId"" UUID REFERENCES ""Applications""(""Id""),
                ""CandidateId"" UUID REFERENCES ""Candidates""(""Id""),
                ""EmailType"" VARCHAR(50) NOT NULL,
                ""ToEmail"" VARCHAR(255) NOT NULL,
                ""Subject"" VARCHAR(500),
                ""Body"" TEXT,
                ""SentAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""Status"" VARCHAR(50),
                ""ErrorMessage"" TEXT
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Recruitment tables created successfully");
    }
}
