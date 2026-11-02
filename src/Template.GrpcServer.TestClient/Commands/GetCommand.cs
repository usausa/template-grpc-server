namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

// Unary
[Command("get", "Get data")]
public sealed class GetCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    [Option<long>("--id", "-i", Description = "id", Required = true)]
    public long Id { get; set; }

    public GetCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var item = await client.GetDataAsync(new GetDataRequest { Id = Id }, cancellationToken: context.CancellationToken);
        Console.WriteLine($"OK {item.Id} {item.Name} {item.Value} {item.CreatedAt.ToDateTime():O}");
    }
}
