using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tenant.Application.Interfaces;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public TenantContextMiddleware(
        RequestDelegate next,
        ILogger<TenantContextMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant resolution for swagger and health endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/swagger") || path.Contains("/health"))
        {
            await _next(context);
            return;
        }

        // Allow OPTIONS requests (CORS preflight) to pass through
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        string? tenantCode = null;

        // For login endpoint, ALWAYS get tenant code from request body first (highest priority)
        if (path.Contains("/auth/login") && context.Request.Method == "POST")
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            _logger.LogInformation("Login request body: {Body}", body);

            try
            {
                if (!string.IsNullOrWhiteSpace(body))
                {
                    var json = JsonDocument.Parse(body);
                    // Try both camelCase and PascalCase
                    if (json.RootElement.TryGetProperty("tenantCode", out var tc))
                    {
                        tenantCode = tc.GetString();
                        _logger.LogInformation("Got tenant code from request body (camelCase): {TenantCode}", tenantCode);
                    }
                    else if (json.RootElement.TryGetProperty("TenantCode", out var tcPascal))
                    {
                        tenantCode = tcPascal.GetString();
                        _logger.LogInformation("Got tenant code from request body (PascalCase): {TenantCode}", tenantCode);
                    }
                    else
                    {
                        _logger.LogWarning("tenantCode property not found in request body. Available properties: {Props}", 
                            string.Join(", ", json.RootElement.EnumerateObject().Select(p => p.Name)));
                    }
                }
                else
                {
                    _logger.LogWarning("Login request body is empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse login request body: {Body}", body);
            }
        }

        // If not login endpoint or no tenant in body, try header
        if (string.IsNullOrEmpty(tenantCode) && context.Request.Headers.TryGetValue("X-Tenant-Code", out var headerTenantCode))
        {
            tenantCode = headerTenantCode.ToString();
            _logger.LogInformation("Got tenant code from header: {TenantCode}", tenantCode);
        }

        // Try to get from JWT claims
        if (string.IsNullOrEmpty(tenantCode))
        {
            tenantCode = context.User?.FindFirst("tenantCode")?.Value;
            if (!string.IsNullOrEmpty(tenantCode))
            {
                _logger.LogInformation("Got tenant code from JWT: {TenantCode}", tenantCode);
            }
        }

        if (string.IsNullOrEmpty(tenantCode))
        {
            _logger.LogWarning("No tenant code provided for request: {Path}", path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant code is required" });
            return;
        }

        _logger.LogInformation("Processing request for tenant: {TenantCode}", tenantCode);

        // Look up tenant in platform database
        var platformConnectionString = GetPlatformConnectionString();
        string? tenantDbName = null;

        try
        {
            await using var connection = new NpgsqlConnection(platformConnectionString);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT \"DbName\" FROM \"Tenants\" WHERE \"TenantCode\" = @code AND \"Status\" = 1",
                connection);
            cmd.Parameters.AddWithValue("code", tenantCode);

            tenantDbName = (string?)await cmd.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up tenant: {TenantCode}", tenantCode);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Failed to resolve tenant" });
            return;
        }

        if (string.IsNullOrEmpty(tenantDbName))
        {
            _logger.LogWarning("Tenant not found or not active: {TenantCode}", tenantCode);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{tenantCode}' not found or not active. Please verify the tenant code or contact support." });
            return;
        }

        // Verify database exists before proceeding
        var masterConnectionString = GetMasterConnectionString();
        var dbExists = await VerifyDatabaseExistsAsync(masterConnectionString, tenantDbName);
        
        if (!dbExists)
        {
            _logger.LogError("Tenant database does not exist: {DbName} for tenant: {TenantCode}", tenantDbName, tenantCode);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { 
                error = $"Tenant database not found. The tenant '{tenantCode}' may not have been fully provisioned. Please contact support or re-register the tenant." 
            });
            return;
        }

        // Build tenant connection string and set it in the connection string provider
        var tenantConnectionString = BuildConnectionString(tenantDbName);
        var connectionStringProvider = context.RequestServices.GetRequiredService<ITenantConnectionStringProvider>();
        connectionStringProvider.SetConnectionString(tenantConnectionString);

        // Ensure schema is up to date
        await EnsureSchemaAsync(tenantConnectionString, tenantDbName);
        
        // Ensure recruitment tables exist (for tenants created before recruitment module)
        if (path.Contains("/recruitment"))
        {
            try
            {
                await SchemaMigrationHelper.EnsureRecruitmentTablesExistAsync(tenantConnectionString, _logger, CancellationToken.None);
                // Also ensure Candidate table has all required columns
                await SchemaUpdateHelper.EnsureCandidateColumnsExistAsync(tenantConnectionString, _logger, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure recruitment tables exist, but continuing");
            }
        }

        await _next(context);
    }

    private string GetPlatformConnectionString()
    {
        var host = _configuration["Database:Host"] ?? "localhost";
        var port = _configuration["Database:Port"] ?? "5432";
        var user = _configuration["Database:User"] ?? "sa";
        var password = _configuration["Database:Password"] ?? "sa";
        var database = _configuration["Database:PlatformDatabase"] ?? "platform_db";
        return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
    }

    private string GetMasterConnectionString()
    {
        var host = _configuration["Database:Host"] ?? "localhost";
        var port = _configuration["Database:Port"] ?? "5432";
        var user = _configuration["Database:User"] ?? "sa";
        var password = _configuration["Database:Password"] ?? "sa";
        return $"Host={host};Port={port};Database=postgres;Username={user};Password={password}";
    }

    private string BuildConnectionString(string dbName)
    {
        var host = _configuration["Database:Host"] ?? "localhost";
        var port = _configuration["Database:Port"] ?? "5432";
        var user = _configuration["Database:User"] ?? "sa";
        var password = _configuration["Database:Password"] ?? "sa";
        return $"Host={host};Port={port};Database={dbName};Username={user};Password={password}";
    }

    private async Task<bool> VerifyDatabaseExistsAsync(string masterConnectionString, string dbName)
    {
        try
        {
            await using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @dbName",
                connection);
            cmd.Parameters.AddWithValue("dbName", dbName);
            
            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying database existence: {DbName}", dbName);
            return false;
        }
    }

    private async Task EnsureSchemaAsync(string connectionString, string dbName)
    {
        const string schemaUpdateSql = @"
            ALTER TABLE IF EXISTS ""UserRoles""
            ADD COLUMN IF NOT EXISTS ""AssignedAtUtc"" TIMESTAMP NOT NULL DEFAULT NOW();
            
            ALTER TABLE IF EXISTS ""RefreshTokens""
            ADD COLUMN IF NOT EXISTS ""RevokedAtUtc"" TIMESTAMP;
            
            ALTER TABLE IF EXISTS ""RefreshTokens""
            ADD COLUMN IF NOT EXISTS ""RevokedReason"" TEXT;
        ";

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(schemaUpdateSql, connection);
            await command.ExecuteNonQueryAsync();
            _logger.LogDebug("Schema updates applied for tenant {DbName}", dbName);
        }
        catch (Exception ex)
        {
            // Only log as warning if it's not a "database does not exist" error
            if (!ex.Message.Contains("does not exist") && !ex.Message.Contains("3D000"))
            {
                _logger.LogWarning(ex, "Schema update warning for tenant {DbName}", dbName);
            }
            else
            {
                _logger.LogError(ex, "Database does not exist for tenant {DbName}", dbName);
                throw; // Re-throw database not found errors
            }
        }
    }
}
