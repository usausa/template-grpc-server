namespace Template.GrpcServer.Host.Application.ExceptionHandling;

using System.Diagnostics;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Template.GrpcServer.Infrastructure.Storage;

public sealed class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> log;

    private readonly TimeProvider timeProvider;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> log, TimeProvider timeProvider)
    {
        this.log = log;
        this.timeProvider = timeProvider;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            AddTraceId(context);
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            throw HandleException(context, ex);
        }
#pragma warning restore CA1031
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (RpcException)
        {
            AddTraceId(context);
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            throw HandleException(context, ex);
        }
#pragma warning restore CA1031
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (RpcException)
        {
            AddTraceId(context);
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            throw HandleException(context, ex);
        }
#pragma warning restore CA1031
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (RpcException)
        {
            AddTraceId(context);
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            throw HandleException(context, ex);
        }
#pragma warning restore CA1031
    }

    private RpcException HandleException(ServerCallContext context, Exception ex)
    {
        // クライアントの切断(ストリーミング中の中断など)は失敗にしない
        if (context.CancellationToken.IsCancellationRequested)
        {
            if (context.Deadline <= timeProvider.GetUtcNow().UtcDateTime)
            {
                log.InfoGrpcCallDeadlineExceeded(context.Method);
                return new RpcException(new Status(StatusCode.DeadlineExceeded, "Deadline exceeded."));
            }

            log.InfoGrpcCallCancelled(context.Method);
            return new RpcException(new Status(StatusCode.Cancelled, "Call cancelled."));
        }

        if (ex is StorageException)
        {
            AddTraceId(context);
            return new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }

        log.ErrorUnhandledException(ex, context.Method);
        AddTraceId(context);
        return new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
    }

    // Web系のProblemDetailsのtraceIdに相当。エラー応答のtrailerに載せる
    private static void AddTraceId(ServerCallContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (traceId is not null)
        {
            context.ResponseTrailers.Add("trace-id", traceId);
        }
    }
}
