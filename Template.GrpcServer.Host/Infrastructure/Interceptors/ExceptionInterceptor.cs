namespace Template.GrpcServer.Host.Infrastructure.Interceptors;

using Grpc.Core;
using Grpc.Core.Interceptors;

public sealed class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> log;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> log)
    {
        this.log = log;
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
        log.ErrorUnhandledException(ex, context.Method);
        return new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
    }
}
