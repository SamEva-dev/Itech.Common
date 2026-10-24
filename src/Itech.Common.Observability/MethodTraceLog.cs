using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog;

namespace Itech.Common.Observability;

/// <summary>Opt-in method tracing analogous to MobiOne, without serializing arguments or results.</summary>
public static class MethodTraceLog
{
    private sealed record Frame(string Name, long Start, Frame? Parent);
    private static readonly AsyncLocal<Frame?> Current = new();

    public static void Begin([CallerMemberName] string member = "", [CallerFilePath] string file = "")
    {
        var name = $"{Path.GetFileNameWithoutExtension(file)}.{member}";
        Current.Value = new Frame(name, Stopwatch.GetTimestamp(), Current.Value);
        Log.ForContext("EventType", "MethodBegin").ForContext("Method", name)
            .Debug("BEGIN {Method}", name);
    }

    public static void End([CallerMemberName] string member = "", [CallerFilePath] string file = "")
    {
        var name = $"{Path.GetFileNameWithoutExtension(file)}.{member}";
        var frame = Current.Value;
        if (frame is null || frame.Name != name)
        {
            Log.Warning("END without matching BEGIN for {Method}", name);
            return;
        }
        Current.Value = frame.Parent;
        var elapsed = Stopwatch.GetElapsedTime(frame.Start).TotalMilliseconds;
        Log.ForContext("EventType", "MethodEnd").ForContext("Method", name)
            .ForContext("ElapsedMs", elapsed)
            .Debug("END {Method} ({ElapsedMs:F2} ms)", name, elapsed);
    }
}
