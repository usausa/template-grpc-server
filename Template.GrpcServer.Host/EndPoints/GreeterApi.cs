namespace Template.GrpcServer.Host.EndPoints;

using Grpc.Core;

public sealed class GreeterApi(ILogger<GreeterApi> logger) : Greeter.GreeterBase
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
