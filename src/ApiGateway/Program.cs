using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------- Ocelot Configuration ----------
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var ocelotOverrides = BuildOcelotOverrides(builder.Configuration);
if (ocelotOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(ocelotOverrides);
}

builder.Services.AddOcelot(builder.Configuration);

// ---------- CORS (for React frontend) ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    app.Urls.Add($"http://0.0.0.0:{renderPort}");
}

app.UseCors("AllowFrontend");

// ---------- Gateway Health Check ----------
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }));

await app.UseOcelot();

app.Run();

static Dictionary<string, string> BuildOcelotOverrides(IConfiguration config)
{
    var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    ApplyServiceRouteOverride(overrides, config, "AuthService:Url", new[] { 0 });
    ApplyServiceRouteOverride(overrides, config, "CitizenService:Url", new[] { 1, 2 });
    ApplyServiceRouteOverride(overrides, config, "ServiceRequestService:Url", new[] { 3, 4 });
    ApplyServiceRouteOverride(overrides, config, "DocumentService:Url", new[] { 5, 6 });

    var publicBaseUrl = config["Gateway__BaseUrl"];
    if (!string.IsNullOrWhiteSpace(publicBaseUrl))
    {
        overrides["GlobalConfiguration:BaseUrl"] = publicBaseUrl;
    }

    return overrides;
}

static void ApplyServiceRouteOverride(
    IDictionary<string, string> overrides,
    IConfiguration config,
    string endpointKey,
    IEnumerable<int> routeIndexes)
{
    var endpoint = config[endpointKey]
        ?? config[endpointKey.Replace(":", "__")];
    if (!TryParseEndpoint(endpoint, out var scheme, out var host, out var port))
    {
        return;
    }

    foreach (var routeIndex in routeIndexes)
    {
        overrides[$"Routes:{routeIndex}:DownstreamScheme"] = scheme;
        overrides[$"Routes:{routeIndex}:DownstreamHostAndPorts:0:Host"] = host;
        overrides[$"Routes:{routeIndex}:DownstreamHostAndPorts:0:Port"] = port.ToString();
    }
}

static bool TryParseEndpoint(string? endpoint, out string scheme, out string host, out int port)
{
    scheme = "http";
    host = string.Empty;
    port = 80;

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        return false;
    }

    var normalizedEndpoint = endpoint.Contains("://", StringComparison.Ordinal)
        ? endpoint
        : $"http://{endpoint}";

    if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
    {
        return false;
    }

    scheme = uri.Scheme;
    host = uri.Host;
    port = uri.IsDefaultPort
        ? (string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80)
        : uri.Port;

    return true;
}
