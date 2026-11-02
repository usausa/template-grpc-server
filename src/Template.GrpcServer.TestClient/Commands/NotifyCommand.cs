namespace Template.GrpcServer.TestClient.Commands;

using Grpc.Core;

using Smart.CommandLine.Hosting;

// Bidirectional streaming(サーバー主体)。参加して配信を表示し、標準入力の各行を全員へ配信する
[Command("notify", "Subscribe server notifications and broadcast stdin lines")]
public sealed class NotifyCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    [Option<string>("--name", "-n", Description = "name", DefaultValue = "client")]
    public string Name { get; set; } = default!;

    public NotifyCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var call = client.Notify(cancellationToken: context.CancellationToken);
        await Task.WhenAll(
            SendAsync(call, Name, context.CancellationToken),
            ReceiveAsync(call, context.CancellationToken));
        Console.WriteLine("OK");
    }

    private static async Task SendAsync(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> call, string name, CancellationToken cancellationToken)
    {
        await call.RequestStream.WriteAsync(new ClientMessage { Join = new JoinRequest { Name = name } }, cancellationToken);
        while (await Console.In.ReadLineAsync(cancellationToken) is { } line)
        {
            await call.RequestStream.WriteAsync(new ClientMessage { Broadcast = new BroadcastRequest { Message = line } }, cancellationToken);
        }

        await call.RequestStream.CompleteAsync();
    }

    private static async Task ReceiveAsync(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> call, CancellationToken cancellationToken)
    {
        await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            switch (message.PayloadCase)
            {
                case ServerMessage.PayloadOneofCase.Tick:
                    Console.WriteLine($"[tick] {message.Tick.Time.ToDateTime():O} subscribers={message.Tick.Subscribers}");
                    break;
                case ServerMessage.PayloadOneofCase.Broadcast:
                    Console.WriteLine($"[{message.Broadcast.Name}] {message.Broadcast.Message}");
                    break;
            }
        }
    }
}
