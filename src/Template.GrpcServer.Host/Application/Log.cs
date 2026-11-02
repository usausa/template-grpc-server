namespace Template.GrpcServer.Host.Application;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Service start.")]
    public static partial void InfoServiceStart(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Runtime: os=[{osDescription}], framework=[{frameworkDescription}], rid=[{runtimeIdentifier}]")]
    public static partial void InfoServiceSettingsRuntime(this ILogger logger, string osDescription, string frameworkDescription, string runtimeIdentifier);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment: version=[{version}], directory=[{directory}]")]
    public static partial void InfoServiceSettingsEnvironment(this ILogger logger, Version? version, string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "GCSettings: serverGC=[{isServerGC}], latencyMode=[{latencyMode}], largeObjectHeapCompactionMode=[{largeObjectHeapCompactionMode}]")]
    public static partial void InfoServiceSettingsGC(this ILogger logger, bool isServerGC, GCLatencyMode latencyMode, GCLargeObjectHeapCompactionMode largeObjectHeapCompactionMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "ThreadPool: workerThreads=[{workerThreads}], completionPortThreads=[{completionPortThreads}]")]
    public static partial void InfoServiceSettingsThreadPool(this ILogger logger, int workerThreads, int completionPortThreads);

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry: otelEndPoint=[{otelEndPoint}], prometheusUri=[{prometheusUri}]")]
    public static partial void InfoServiceSettingsTelemetry(this ILogger logger, string otelEndPoint, string prometheusUri);

    // gRPC

    [LoggerMessage(Level = LogLevel.Information, Message = "gRPC call end. method=[{method}], status=[{status}], elapsed=[{elapsed}]")]
    public static partial void InfoGrpcCallEnd(this ILogger logger, string method, Grpc.Core.StatusCode status, long elapsed);

    [LoggerMessage(Level = LogLevel.Information, Message = "gRPC call cancelled. method=[{method}]")]
    public static partial void InfoGrpcCallCancelled(this ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Information, Message = "gRPC call deadline exceeded. method=[{method}]")]
    public static partial void InfoGrpcCallDeadlineExceeded(this ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Long execution. method=[{method}], elapsed=[{elapsed}]")]
    public static partial void WarnLongExecution(this ILogger logger, string method, long elapsed);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception. method=[{method}]")]
    public static partial void ErrorUnhandledException(this ILogger logger, Exception ex, string method);
}
