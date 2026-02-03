using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Validators;
using Platform.Application.DTOs;
using Platform.Application.Interfaces;
using Platform.Application.Services;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Factories;
using Platform.Infrastructure.Repositories;
using Platform.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/platform-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterTenantRequestValidator>();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("PlatformDb") 
    ?? $"Host={builder.Configuration["Database:Host"] ?? "localhost"};Port={builder.Configuration["Database:Port"] ?? "5432"};Database={builder.Configuration["Database:PlatformDatabase"] ?? "platform_db"};Username={builder.Configuration["Database:User"] ?? "sa"};Password={builder.Configuration["Database:Password"] ?? "sa"}";

builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add repositories and services
builder.Services.AddScoped<ITenantsRepository, TenantsRepository>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<TenantSchemaCreator>();
builder.Services.AddScoped<TenantProvisioningService>();

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

// Ensure database exists and apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Log.Information("Platform database migrated successfully");
        
        // Ensure AdminEmail column exists (for email-based tenant lookup)
        await context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Tenants"" ADD COLUMN IF NOT EXISTS ""AdminEmail"" VARCHAR(255);
        ");
        Log.Information("Ensured AdminEmail column exists");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply migrations. Database may already exist.");
        await context.Database.EnsureCreatedAsync();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

Log.Information("Platform API starting on port 5000...");
app.Run();
