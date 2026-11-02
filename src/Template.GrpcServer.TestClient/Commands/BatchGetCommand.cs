namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Unary
[Command("batch-get", "Get data by ids")]
public sealed class BatchGetCommand : ICommandHandler
{
    private readonly TimeProvider timeProvider;

    private readonly ClientSetting setting;

    private readonly Data.DataClient client;

    [Option<string>("--ids", "-i", Description = "ids (comma separated)", Required = true)]
    public string Ids { get; set; } = default!;

    public BatchGetCommand(TimeProvider timeProvider, ClientSetting setting, Data.DataClient client)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var request = new BatchGetDataRequest();
        request.Ids.AddRange(Ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static x => Int64.Parse(x, CultureInfo.InvariantCulture)));
        var deadline = timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(setting.Deadline);
        var reply = await client.BatchGetDataAsync(request, deadline: deadline, cancellationToken: context.CancellationToken);
        foreach (var item in reply.Items)
        {
            Console.WriteLine($"{item.Id} {item.Name} {item.Value} {item.CreatedAt.ToDateTime():O}");
        }

        Console.WriteLine($"OK {reply.Items.Count}");
    }
}
