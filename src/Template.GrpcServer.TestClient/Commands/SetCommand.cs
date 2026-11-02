namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

// Unary(検証エラーと重複はgoogle.rpc.Statusの詳細付きで返る)
[Command("set", "Set data")]
public sealed class SetCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    [Option<string>("--name", "-n", Description = "name", Required = true)]
    public string Name { get; set; } = default!;

    [Option<int>("--value", "-v", Description = "value", Required = true)]
    public int Value { get; set; }

    public SetCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var reply = await client.SetDataAsync(new SetDataRequest { Name = Name, Value = Value }, cancellationToken: context.CancellationToken);
        Console.WriteLine($"OK {reply.Id}");
    }
}
