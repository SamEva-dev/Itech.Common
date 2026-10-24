using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Itech.Common.Observability;

public sealed class ApiActionLoggingFilter(ILogger<ApiActionLoggingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.DisplayName ?? "unknown";
        var start = Stopwatch.GetTimestamp();
        var failed = false;
        logger.LogDebug("MVC {EventType} {ActionName}", "MethodBegin", actionName);
        try
        {
            var result = await next();
            failed = result.Exception is not null && !result.ExceptionHandled;
        }
        catch { failed = true; throw; }
        finally
        {
            logger.LogDebug("MVC {EventType} {ActionName} {ElapsedMs} {Failed}",
                "MethodEnd", actionName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, failed);
        }
    }
}
