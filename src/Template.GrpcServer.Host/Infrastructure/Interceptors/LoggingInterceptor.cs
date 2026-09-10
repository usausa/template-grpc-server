namespace Template.GrpcServer.Host.Infrastructure.Interceptors;

using Grpc.Core;
using Grpc.Core.Interceptors;

public sealed class LoggingInterceptor : Interceptor
{
    private readonly ILogger<LoggingInterceptor> log;

    private readonly TimeProvider timeProvider;

    public LoggingInterceptor(ILogger<LoggingInterceptor> log, TimeProvider timeProvider)
    {
        this.log = log;
        this.timeProvider = timeProvider;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        try
        {
            return await continuation(request, context);
        }
        finally
        {
            LogEnd(context, timestamp);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        try
        {
            await continuation(request, responseStream, context);
        }
        finally
        {
            LogEnd(context, timestamp);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        try
        {
            return await continuation(requestStream, context);
        }
        finally
        {
            LogEnd(context, timestamp);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        finally
        {
            LogEnd(context, timestamp);
        }
    }

    private void LogEnd(ServerCallContext context, long timestamp)
    {
        var elapsed = (long)timeProvider.GetElapsedTime(timestamp).TotalMilliseconds;
        log.InfoGrpcCallEnd(context.Method, context.Status.StatusCode, elapsed);
    }
}
