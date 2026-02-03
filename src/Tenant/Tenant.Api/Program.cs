using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Tenant.Application.Interfaces;
using Tenant.Infrastructure.Data;
using Tenant.Infrastructure.Services;
using Tenant.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/tenant-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add connection string provider
builder.Services.AddScoped<ITenantConnectionStringProvider, TenantConnectionStringProvider>();

// Add DbContext factory that reads connection string from provider
builder.Services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
{
    var connectionStringProvider = serviceProvider.GetRequiredService<ITenantConnectionStringProvider>();
    var connectionString = connectionStringProvider.GetConnectionString()
        ?? "Host=localhost;Port=5432;Database=placeholder;Username=sa;Password=sa";
    options.UseNpgsql(connectionString);
});

// Add authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-256-bit-secret-key-here-must-be-at-least-32-characters-long";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "HrSaaS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "HrSaaS",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Add services
builder.Services.AddScoped<IAuthService, AuthService>();

// Add HttpContextAccessor for middleware
builder.Services.AddHttpContextAccessor();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Add global exception handler (should be early in pipeline)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Add tenant context middleware (must be before authentication)
app.UseMiddleware<TenantContextMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("Tenant API starting on port 5001...");
app.Run();
