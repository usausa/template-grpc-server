namespace Template.GrpcServer.TestClient.Infrastructure.Interceptors;

using Grpc.Core;
using Grpc.Core.Interceptors;

// 呼出の開始と、Unaryの結果(状態・所要時間)を記録する。ストリーミングは終わりが呼出側で決まるため開始だけ
public sealed class LoggingInterceptor : Interceptor
{
    private readonly ILogger<LoggingInterceptor> log;

    private readonly TimeProvider timeProvider;

    public LoggingInterceptor(ILogger<LoggingInterceptor> log, TimeProvider timeProvider)
    {
        this.log = log;
        this.timeProvider = timeProvider;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        log.InfoGrpcCallStart(context.Method.FullName);

        var timestamp = timeProvider.GetTimestamp();
        var call = continuation(request, context);
        return new AsyncUnaryCall<TResponse>(
            LogResponseAsync(call.ResponseAsync, context.Method.FullName, timestamp),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        log.InfoGrpcCallStart(context.Method.FullName);
        return continuation(request, context);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        log.InfoGrpcCallStart(context.Method.FullName);
        return continuation(context);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        log.InfoGrpcCallStart(context.Method.FullName);
        return continuation(context);
    }

    private async Task<TResponse> LogResponseAsync<TResponse>(Task<TResponse> responseTask, string method, long timestamp)
    {
        var status = StatusCode.OK;
        try
        {
            return await responseTask;
        }
        catch (RpcException ex)
        {
            status = ex.StatusCode;
            throw;
        }
        finally
        {
            var elapsed = (long)timeProvider.GetElapsedTime(timestamp).TotalMilliseconds;
            log.InfoGrpcCallEnd(method, status, elapsed);
        }
    }
}
