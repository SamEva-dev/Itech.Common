using System.Diagnostics;
using System.Reflection;
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

namespace Itech.Common.Observability;

/// <summary>
/// Sets up the standard Itech logging pipeline in C# rather than loading a list of
/// Serilog extensions by assembly name from appsettings. Optional sinks are loaded
/// only when explicitly enabled.
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddItechObservability(this WebApplicationBuilder builder, string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("An application name is required", nameof(applicationName));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
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

        var seqEnabled = builder.Configuration.GetValue<bool>("ItechLogging:Seq:Enabled");
        var seqUrl = builder.Configuration["ItechLogging:Seq:ServerUrl"];
        var pgEnabled = builder.Configuration.GetValue<bool>("ItechLogging:PostgreSql:Enabled");
        var pgConnection = builder.Configuration.GetConnectionString("ItechLogging");

        // Validate optional sinks early, providing a useful error only if they are enabled.
        if (seqEnabled)
        {
            if (!Uri.TryCreate(seqUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                throw new InvalidOperationException("ItechLogging:Seq:ServerUrl must be an HTTP(S) URL when Seq is enabled.");
            EnsureAssemblyAvailable("Serilog.Sinks.Seq");
        }
        if (pgEnabled)
        {
            if (string.IsNullOrWhiteSpace(pgConnection))
                throw new InvalidOperationException("ConnectionStrings:ItechLogging is required when PostgreSQL log sink is enabled.");
            EnsureAssemblyAvailable("Serilog.Sinks.PostgreSQL");
            EnsureAssemblyAvailable("Serilog.Sinks.PostgreSQL.Configuration");
        }

        builder.Host.UseSerilog((ctx, services, logger) =>
        {
            var config = ctx.Configuration;
            logger
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                .MinimumLevel.Override("VPSControlCenter", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .ReadFrom.Services(services)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] [{CorrelationId}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Logger(child => child
                    .Filter.ByExcluding(IsEntityFrameworkEvent)
                    .WriteTo.File(appPath,
                        fileSizeLimitBytes: 104857600,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 100,
                        outputTemplate:
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"))
                .WriteTo.Logger(child => child
                    .Filter.ByIncludingOnly(IsEntityFrameworkEvent)
                    .WriteTo.File(efPath,
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        fileSizeLimitBytes: 104857600,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 100,
                        outputTemplate:
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"));

            // Configure only the external sinks the host explicitly enables. The main
            // pipeline never invokes ReadFrom.Configuration() on application appsettings.
            if (seqEnabled)
            {
                logger.ReadFrom.Configuration(BuildOptionalSinkConfiguration(
                    "Serilog.Sinks.Seq", "Seq",
                    ("serverUrl", seqUrl!)));
            }
            if (pgEnabled)
            {
                // Preserve the existing PostgreSQL configuration convention.
                logger.ReadFrom.Configuration(BuildOptionalSinkConfiguration(
                    "Serilog.Sinks.PostgreSQL.Configuration", "PostgreSQL",
                    ("connectionString", pgConnection!),
                    ("tableName", "AppLogs"),
                    ("needAutoCreateTable", "false"),
                    ("schemaName", "public"),
                    ("useCopy", "false"),
                    ("restrictedToMinimumLevel", "Warning")));
            }
        });

        builder.Logging.Configure(options => options.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId |
            ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Baggage);

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

    private static bool IsEntityFrameworkEvent(LogEvent evt)
    {
        if (!evt.Properties.TryGetValue("SourceContext", out var value)) return false;
        return value is ScalarValue { Value: string source }
            && source.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal);
    }

    private static IConfiguration BuildOptionalSinkConfiguration(
        string assemblyName, string sinkName, params (string Key, string Value)[] args)
    {
        var entries = new Dictionary<string, string?>
        {
            ["Serilog:Using:0"] = assemblyName,
            ["Serilog:WriteTo:0:Name"] = sinkName
        };
        foreach (var (key, value) in args)
            entries[$"Serilog:WriteTo:0:Args:{key}"] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(entries).Build();
    }

    private static void EnsureAssemblyAvailable(string assemblyName)
    {
        try { _ = Assembly.Load(new AssemblyName(assemblyName)); }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException(
                $"Optional logging sink '{assemblyName}' is enabled but its assembly is missing. " +
                "Restore/publish the sink package or disable it under ItechLogging.", ex);
        }
    }

    private static string NormalizeEnvironmentName(string name) =>
        new string(name.ToUpperInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}
