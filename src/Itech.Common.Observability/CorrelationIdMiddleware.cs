using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Itech.Common.Observability;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var id = !string.IsNullOrWhiteSpace(incoming) && incoming.Length <= 64 && ValidId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = id;
        context.Response.OnStarting(() => { context.Response.Headers[HeaderName] = id; return Task.CompletedTask; });
        Activity.Current?.SetTag("correlation.id", id);
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            await next(context);
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidId();
}
