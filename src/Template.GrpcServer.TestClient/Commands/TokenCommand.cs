namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Infrastructure.Tokens;

[Command("token", "Print access token")]
public sealed class TokenCommand : ICommandHandler
{
    private readonly TokenProvider tokenProvider;

    public TokenCommand(TokenProvider tokenProvider)
    {
        this.tokenProvider = tokenProvider;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine(tokenProvider.GetToken());
        return ValueTask.CompletedTask;
    }
}
