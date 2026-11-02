namespace Template.GrpcServer.TestClient.Commands;

using Grpc.Core;

using Smart.CommandLine.Hosting;

// Server streaming
[Command("list", "List data")]
public sealed class ListCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    [Option<string>("--name", "-n", Description = "name filter")]
    public string? Name { get; set; }

    [Option<string>("--sort", "-s", Description = "sort column (Id / Name / Value / CreatedAt)")]
    public string? Sort { get; set; }

    [Option<bool>("--desc", "-d", Description = "sort descending")]
    public bool Desc { get; set; }

    public ListCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var request = new ListDataRequest { Name = Name ?? string.Empty, Sort = Sort ?? string.Empty, Desc = Desc };
        using var call = client.ListData(request, cancellationToken: context.CancellationToken);
        var count = 0;
        await foreach (var item in call.ResponseStream.ReadAllAsync(context.CancellationToken))
        {
            Console.WriteLine($"{item.Id} {item.Name} {item.Value} {item.CreatedAt.ToDateTime():O}");
            count++;
        }

        Console.WriteLine($"OK {count}");
    }
}
