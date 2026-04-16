using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceRequestService.Data;
using ServiceRequestService.Middleware;
using ServiceRequestService.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbHost = builder.Configuration["RequestDb:Host"]
        ?? throw new InvalidOperationException("Request DB host is not configured.");
    var dbPort = builder.Configuration["RequestDb:Port"] ?? "5432";
    var dbName = builder.Configuration["RequestDb:Database"] ?? "request_db";
    var dbUser = builder.Configuration["RequestDb:Username"] ?? "postgres";
    var dbPassword = builder.Configuration["RequestDb:Password"]
        ?? throw new InvalidOperationException("Request DB password is not configured.");

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
}

builder.Services.AddDbContext<ServiceRequestDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------- JWT Authentication (shared key with Auth Service) ----------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuthService",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EGovernmentPlatform",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "name",
        RoleClaimType = "role"
    };
});

builder.Services.AddAuthorization();

// ---------- Application Services ----------
builder.Services.AddScoped<IServiceRequestService, ServiceRequestServiceImpl>();
builder.Services.AddHealthChecks();

// ---------- Controllers ----------
builder.Services.AddControllers();

var app = builder.Build();

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    app.Urls.Add($"http://0.0.0.0:{renderPort}");
}

// ---------- Auto-migrate on startup ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>();
    await db.Database.MigrateAsync();
}

// ---------- Middleware Pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
