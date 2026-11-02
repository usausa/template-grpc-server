namespace Template.GrpcServer.TestClient.Commands;

using Grpc.Core;

using Smart.CommandLine.Hosting;

// Bidirectional streaming(エコー)。標準入力の各行を送り、応答を表示する
[Command("chat", "Chat (echo) with stdin lines")]
public sealed class ChatCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    [Option<string>("--name", "-n", Description = "name", DefaultValue = "client")]
    public string Name { get; set; } = default!;

    public ChatCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var call = client.Chat(cancellationToken: context.CancellationToken);
        await Task.WhenAll(
            SendAsync(call, Name, context.CancellationToken),
            ReceiveAsync(call, context.CancellationToken));
        Console.WriteLine("OK");
    }

    private static async Task SendAsync(AsyncDuplexStreamingCall<ChatMessage, ChatMessage> call, string name, CancellationToken cancellationToken)
    {
        while (await Console.In.ReadLineAsync(cancellationToken) is { } line)
        {
            await call.RequestStream.WriteAsync(new ChatMessage { Name = name, Message = line }, cancellationToken);
        }

        await call.RequestStream.CompleteAsync();
    }

    private static async Task ReceiveAsync(AsyncDuplexStreamingCall<ChatMessage, ChatMessage> call, CancellationToken cancellationToken)
    {
        await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            Console.WriteLine($"{message.Name}: {message.Message}");
        }
    }
}
