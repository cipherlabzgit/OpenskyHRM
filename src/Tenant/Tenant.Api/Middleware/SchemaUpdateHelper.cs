using Npgsql;

namespace Tenant.Api.Middleware;

public static class SchemaUpdateHelper
{
    public static async Task EnsureCandidateColumnsExistAsync(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Add missing columns to Candidates table if they don't exist
            var updateSql = @"
                ALTER TABLE IF EXISTS ""Candidates""
                ADD COLUMN IF NOT EXISTS ""ReferralCode"" VARCHAR(50);
                
                ALTER TABLE IF EXISTS ""Candidates""
                ADD COLUMN IF NOT EXISTS ""Website"" VARCHAR(255);
            ";

            await using var cmd = new NpgsqlCommand(updateSql, connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Ensured Candidate table has ReferralCode and Website columns");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error ensuring Candidate columns exist");
            // Don't throw - this is a best-effort update
        }
    }
}
