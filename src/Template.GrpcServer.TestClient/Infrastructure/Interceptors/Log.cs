namespace Template.GrpcServer.TestClient.Infrastructure.Interceptors;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "gRPC call start. method=[{method}]")]
    public static partial void InfoGrpcCallStart(this ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Information, Message = "gRPC call end. method=[{method}], status=[{status}], elapsed=[{elapsed}]")]
    public static partial void InfoGrpcCallEnd(this ILogger logger, string method, Grpc.Core.StatusCode status, long elapsed);
}
