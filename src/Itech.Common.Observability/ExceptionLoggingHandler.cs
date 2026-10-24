using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Itech.Common.Observability;

public sealed class ExceptionLoggingHandler(ILogger<ExceptionLoggingHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "[unmatched]";
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Client disconnected: {EventType} {HttpMethod} {RouteTemplate} {TraceId}",
                "ClientRequestAborted", context.Request.Method, route, traceId);
            return ValueTask.FromResult(false);
        }
        logger.LogError(exception, "Unhandled API exception: {EventType} {HttpMethod} {RouteTemplate} {TraceId} {ExceptionType}",
            "UnhandledException", context.Request.Method, route, traceId, exception.GetType().FullName);
        return ValueTask.FromResult(false); // preserve existing problem-details handling
    }
}
