using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------- Ocelot Configuration ----------
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// ---------- CORS (for React frontend) ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// ---------- Gateway Health Check ----------
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }));

await app.UseOcelot();

app.Run();
