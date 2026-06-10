using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Data;
using NotificationService.Hubs;
using NotificationService.Middleware;
using NotificationService.Services;
using NotificationService.Services.Events;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
var dbHost = NormalizeDbHost(builder.Configuration["NotificationDb:Host"]);
var dbPort = builder.Configuration["NotificationDb:Port"] ?? "5432";
var dbName = builder.Configuration["NotificationDb:Database"] ?? "notification_db";
var dbUser = builder.Configuration["NotificationDb:Username"] ?? "postgres";
var dbPassword = builder.Configuration["NotificationDb:Password"];
var dbSslMode = builder.Configuration["NotificationDb:SslMode"] ?? "Disable";

var connectionString =
    !string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbPassword)
        ? $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode={dbSslMode}"
        : builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection settings are not configured.");

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000", "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
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

    // SignalR clients cannot send custom headers on WebSocket handshakes,
    // so they pass the bearer token in the access_token query string.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ---------- SignalR ----------
builder.Services.AddSignalR();

// ---------- Application Services ----------
var authServiceBaseUrl = builder.Configuration["AuthService:BaseUrl"] ?? "http://auth_service:8080/";
var internalApiKey = builder.Configuration["Internal:ApiKey"] ?? "CHANGE_ME_INTERNAL_KEY";

builder.Services.AddHttpClient<IUserDirectory, UserDirectory>(client =>
{
    client.BaseAddress = new Uri(authServiceBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
});

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationServiceImpl>();
builder.Services.AddScoped<EventRouter>();
builder.Services.AddHostedService<NotificationConsumer>();
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
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

// ---------- Middleware Pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/healthz");

app.Run();

static string? NormalizeDbHost(string? host)
{
    if (string.IsNullOrWhiteSpace(host))
        return host;

    var cleaned = host.Trim();
    if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        return uri.Host;

    if (cleaned.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        cleaned = cleaned["tcp://".Length..];

    var slashIndex = cleaned.IndexOf('/');
    if (slashIndex >= 0)
        cleaned = cleaned[..slashIndex];

    var colonIndex = cleaned.IndexOf(':');
    if (colonIndex >= 0)
        cleaned = cleaned[..colonIndex];

    return cleaned;
}
