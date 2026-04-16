using System.Text;
using DocumentService.Data;
using DocumentService.Middleware;
using DocumentService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
var dbHost = NormalizeDbHost(builder.Configuration["DocumentDb:Host"]);
var dbPort = builder.Configuration["DocumentDb:Port"] ?? "5432";
var dbName = builder.Configuration["DocumentDb:Database"] ?? "document_db";
var dbUser = builder.Configuration["DocumentDb:Username"] ?? "postgres";
var dbPassword = builder.Configuration["DocumentDb:Password"];
var dbSslMode = builder.Configuration["DocumentDb:SslMode"] ?? "Disable"; // For local Docker: Disable, for cloud: Require

var connectionString =
    !string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbPassword)
        ? $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode={dbSslMode}"
        : builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection settings are not configured.");

builder.Services.AddDbContext<DocumentDbContext>(options =>
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
builder.Services.AddScoped<IDocumentService, DocumentServiceImpl>();
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
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
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

static string? NormalizeDbHost(string? host)
{
    if (string.IsNullOrWhiteSpace(host))
    {
        return host;
    }

    var cleaned = host.Trim();

    if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
    {
        return uri.Host;
    }

    if (cleaned.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
    {
        cleaned = cleaned["tcp://".Length..];
    }

    var slashIndex = cleaned.IndexOf('/');
    if (slashIndex >= 0)
    {
        cleaned = cleaned[..slashIndex];
    }

    var colonIndex = cleaned.IndexOf(':');
    if (colonIndex >= 0)
    {
        cleaned = cleaned[..colonIndex];
    }

    return cleaned;
}
