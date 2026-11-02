namespace Template.GrpcServer.TestClient.Infrastructure.Interceptors;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Template.GrpcServer.TestClient.Infrastructure.Tokens;

// 全呼出にBearerトークンを付ける
public sealed class AuthorizationInterceptor : Interceptor
{
    private readonly TokenProvider tokenProvider;

    public AuthorizationInterceptor(TokenProvider tokenProvider)
    {
        this.tokenProvider = tokenProvider;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, Authorize(context));

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, Authorize(context));

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(Authorize(context));

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(Authorize(context));

    private ClientInterceptorContext<TRequest, TResponse> Authorize<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var headers = context.Options.Headers ?? [];
        headers.Add("Authorization", $"Bearer {tokenProvider.GetToken()}");
        return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithHeaders(headers));
    }
}
