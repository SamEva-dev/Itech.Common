using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Diagnostics;
using System.Reflection;

namespace Itech.Common.Observability;

/// <summary>Shared Serilog bootstrap, sinks, correlation and HTTP diagnostics for Itech APIs.</summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddItechObservability(this WebApplicationBuilder builder, string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("An application name is required", nameof(applicationName));

        // Bootstrap logs startup failures before the host DI container exists.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateBootstrapLogger();

        var home = Environment.GetEnvironmentVariable($"{NormalizeEnvironmentName(applicationName)}_HOME");
        if (string.IsNullOrWhiteSpace(home))
        {
            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(baseFolder)) baseFolder = AppContext.BaseDirectory;
            home = Path.Combine(baseFolder, applicationName);
        }

        var appPath = Path.Combine(home, "log", applicationName, $"{applicationName}Service_log.txt");
        var efPath = Path.Combine(home, "log", applicationName, "EntityFramework", "EntityFramework_log.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(appPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(efPath)!);

        // Matches MobiOne appsettings Serilog:WriteTo ordering. Set paths before ReadFrom.Configuration.
        builder.Configuration["Serilog:WriteTo:1:Args:configureLogger:WriteTo:0:Args:path"] = appPath;
        builder.Configuration["Serilog:WriteTo:2:Args:configureLogger:WriteTo:0:Args:path"] = efPath;
        builder.Configuration["Serilog:Properties:Application"] = applicationName;

        // Optional external sinks: no credentials embedded in committed appsettings.
        var index = 3;
        var seqEnabled = builder.Configuration.GetValue<bool>("ItechLogging:Seq:Enabled");
        var seqUrl = builder.Configuration["ItechLogging:Seq:ServerUrl"];
        if (seqEnabled)
        {
            if (!Uri.TryCreate(seqUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                throw new InvalidOperationException("ItechLogging:Seq:ServerUrl must be a valid HTTP(S) URI when Seq is enabled.");
            EnsureSinkAvailable("Serilog.Sinks.Seq", "Serilog.Sinks.Seq");
            // The optional assembly must only enter Serilog:Using when the sink is enabled.
            AddUsingAssembly(builder.Configuration, "Serilog.Sinks.Seq");
            builder.Configuration[$"Serilog:WriteTo:{index}:Name"] = "Seq";
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:serverUrl"] = seqUrl;
            index++;
        }

        var pgEnabled = builder.Configuration.GetValue<bool>("ItechLogging:PostgreSql:Enabled");
        var pgConnection = builder.Configuration.GetConnectionString("ItechLogging");
        if (pgEnabled)
        {
            if (string.IsNullOrWhiteSpace(pgConnection))
                throw new InvalidOperationException("ConnectionStrings:ItechLogging is required when PostgreSQL log sink is enabled.");
            EnsureSinkAvailable("Serilog.Sinks.PostgreSQL", "Serilog.Sinks.PostgreSQL");
            EnsureSinkAvailable("Serilog.Sinks.PostgreSQL.Configuration", "Serilog.Sinks.PostgreSQL.Configuration");
            AddUsingAssembly(builder.Configuration, "Serilog.Sinks.PostgreSQL");
            AddUsingAssembly(builder.Configuration, "Serilog.Sinks.PostgreSQL.Configuration");
            builder.Configuration[$"Serilog:WriteTo:{index}:Name"] = "PostgreSQL";
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:connectionString"] = pgConnection;
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:tableName"] = "AppLogs";
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:needAutoCreateTable"] = "false";
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:schemaName"] = "public";
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:useCopy"] = "false";
            builder.Configuration[$"Serilog:WriteTo:{index}:Args:restrictedToMinimumLevel"] = "Warning";
        }

        builder.Host.UseSerilog((ctx, services, configuration) => configuration
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services));
        builder.Logging.Configure(options => options.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId |
            ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Baggage);

        // MVC actions only (Minimal API endpoints are covered by UseSerilogRequestLogging).
        builder.Services.AddScoped<ApiActionLoggingFilter>();
        builder.Services.AddExceptionHandler<ExceptionLoggingHandler>();
        return builder;
    }

    public static WebApplication UseItechObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (http, _, error) =>
            {
                if (http.Request.Path.StartsWithSegments("/health") ||
                    http.Request.Path.StartsWithSegments("/metrics") ||
                    http.Request.Path.StartsWithSegments("/api/v1/system/health"))
                    return LogEventLevel.Verbose;
                return error is not null || http.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : http.Response.StatusCode >= 400 ? LogEventLevel.Warning : LogEventLevel.Information;
            };
            options.EnrichDiagnosticContext = (context, http) =>
            {
                var route = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "[unmatched]";
                context.Set("RouteTemplate", route);
                context.Set("CorrelationId", http.TraceIdentifier);
                context.Set("TraceId", Activity.Current?.TraceId.ToString());
                context.Set("UserId", http.User.FindFirst("sub")?.Value);
            };
        });
        return app;
    }

    private static void EnsureSinkAvailable(string assemblyName, string packageName)
    {
        try { _ = Assembly.Load(new AssemblyName(assemblyName)); }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException(
                $"ItechLogging requires {packageName} because its sink is enabled. " +
                "Restore and publish the API with the Itech.Common.Observability project/package reference, " +
                "or disable that sink in ItechLogging configuration.", ex);
        }
    }

    private static void AddUsingAssembly(IConfiguration configuration, string assemblyName)
    {
        var usingSection = configuration.GetSection("Serilog:Using");
        if (usingSection.GetChildren().Any(x => string.Equals(x.Value, assemblyName, StringComparison.Ordinal)))
            return;
        var nextIndex = usingSection.GetChildren()
            .Select(x => int.TryParse(x.Key, out var i) ? i : -1)
            .DefaultIfEmpty(-1).Max() + 1;
        configuration[$"Serilog:Using:{nextIndex}"] = assemblyName;
    }

    private static string NormalizeEnvironmentName(string name)
        => new string(name.ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}
