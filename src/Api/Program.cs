using LocationManagement.Api.Data;
using LocationManagement.Api.Middleware;
using LocationManagement.Api.Repositories;
using LocationManagement.Api.Services;
using LocationManagement.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Wire up shared service defaults: OTel (traces, metrics, logs via OTLP),
// health checks, service discovery, and resilience policies.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register AppDbContext with SQL Server provider and NetTopologySuite spatial support.
// The connection string is injected by Aspire via WithReference in AppHost,
// or via environment variable for local development.
var connectionString = builder.Configuration.GetConnectionString("locationmanagement")
    ?? Environment.GetEnvironmentVariable("LOCATIONMANAGEMENT_CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'locationmanagement' not found in configuration. " +
        "Set LOCATIONMANAGEMENT_CONNECTION_STRING environment variable or configure in appsettings.json");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

// Register core infrastructure services
builder.Services.AddSingleton<ICoordinateReprojectionService, CoordinateReprojectionService>();
// builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();  // TODO: Implement in Section 5
builder.Services.AddSingleton<ILocalFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Register repositories
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IPendingEditRepository, PendingEditRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<INamedShapeRepository, NamedShapeRepository>();

// Register business logic services
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<INamedShapeService, NamedShapeService>();

// Register memory cache for development
builder.Services.AddMemoryCache();

// Configure JWT authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var signingKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
            ?? throw new InvalidOperationException("JWT_SIGNING_KEY environment variable is not set.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = "LocationManagement",
            ValidateAudience = true,
            ValidAudience = "LocationManagementClient",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Configure rate limiting (basic setup for now)
builder.Services.AddRateLimiter(options =>
{
    // Default global rate limiter: 100 requests per minute per user
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Register hosted services for background tasks
builder.Services.AddHostedService<AuditRetentionService>();
builder.Services.AddHostedService<NotificationCleanupService>();

// Register email service
builder.Services.AddSingleton<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Maps /health/live and /health/ready endpoints.
app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
