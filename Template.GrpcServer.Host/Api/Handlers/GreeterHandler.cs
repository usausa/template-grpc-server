namespace Template.GrpcServer.Host.Api.Handlers;

using Grpc.Core;

using Template.GrpcServer.Host;

public sealed class GreeterHandler(ILogger<GreeterHandler> logger) : Greeter.GreeterBase
{
#pragma warning disable CA1848
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        logger.LogInformation("The message is received from {Name}", request.Name);

        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
#pragma warning restore CA1848
}
