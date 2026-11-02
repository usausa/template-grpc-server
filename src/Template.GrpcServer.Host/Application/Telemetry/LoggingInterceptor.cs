namespace Template.GrpcServer.Host.Application.Telemetry;

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
        var status = StatusCode.OK;
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        finally
        {
            LogEnd(context, status, timestamp);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        var status = StatusCode.OK;
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        finally
        {
            LogEnd(context, status, timestamp);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        var status = StatusCode.OK;
        try
        {
            return await continuation(requestStream, context);
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        finally
        {
            LogEnd(context, status, timestamp);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var timestamp = timeProvider.GetTimestamp();
        var status = StatusCode.OK;
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        finally
        {
            LogEnd(context, status, timestamp);
        }
    }

    private void LogEnd(ServerCallContext context, StatusCode status, long timestamp)
    {
        var elapsed = (long)timeProvider.GetElapsedTime(timestamp).TotalMilliseconds;
        log.InfoGrpcCallEnd(context.Method, status, elapsed);
    }
}
