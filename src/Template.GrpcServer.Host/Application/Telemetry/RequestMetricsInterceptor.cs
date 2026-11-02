namespace Template.GrpcServer.Host.Application.Telemetry;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Template.GrpcServer.Host.Settings;

public sealed class RequestMetricsInterceptor : Interceptor
{
    private readonly ILogger<RequestMetricsInterceptor> log;

    private readonly TimeProvider timeProvider;

    private readonly TimeSpan longExecutionThreshold;

    private readonly ApplicationInstrument instrument;

    public RequestMetricsInterceptor(
        ILogger<RequestMetricsInterceptor> log,
        TimeProvider timeProvider,
        TelemetrySetting setting,
        ApplicationInstrument instrument)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        longExecutionThreshold = TimeSpan.FromMilliseconds(setting.LongExecutionThreshold);
        this.instrument = instrument;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var start = timeProvider.GetTimestamp();
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
            instrument.IncrementRequestExecution(context.Method, status);

            var elapsed = timeProvider.GetElapsedTime(start);
            if (elapsed >= longExecutionThreshold)
            {
                instrument.IncrementRequestLongExecution(context.Method, status);
                log.WarnLongExecution(context.Method, (long)elapsed.TotalMilliseconds);
            }
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
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
            instrument.IncrementRequestExecution(context.Method, status);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
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
            instrument.IncrementRequestExecution(context.Method, status);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
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
            instrument.IncrementRequestExecution(context.Method, status);
        }
    }
}
