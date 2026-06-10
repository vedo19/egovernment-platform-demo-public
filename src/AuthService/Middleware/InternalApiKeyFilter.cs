using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthService.Middleware;

public class InternalApiKeyFilter : IAsyncActionFilter
{
    private const string HeaderName = "X-Internal-Api-Key";
    private readonly string? _expectedKey;
    private readonly ILogger<InternalApiKeyFilter> _logger;

    public InternalApiKeyFilter(IConfiguration configuration, ILogger<InternalApiKeyFilter> logger)
    {
        _expectedKey = configuration["Internal:ApiKey"];
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_expectedKey))
        {
            _logger.LogError("Internal:ApiKey is not configured; rejecting internal request.");
            context.Result = new ObjectResult(new { error = "Internal API key not configured." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided)
            || string.IsNullOrWhiteSpace(provided)
            || !string.Equals(provided.ToString(), _expectedKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid internal API key." });
            return;
        }

        await next();
    }
}
