namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Unary
[Command("get", "Get data")]
public sealed class GetCommand : ICommandHandler
{
    private readonly TimeProvider timeProvider;

    private readonly ClientSetting setting;

    private readonly Data.DataClient client;

    [Option<long>("--id", "-i", Description = "id", Required = true)]
    public long Id { get; set; }

    public GetCommand(TimeProvider timeProvider, ClientSetting setting, Data.DataClient client)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var deadline = timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(setting.Deadline);
        var item = await client.GetDataAsync(new GetDataRequest { Id = Id }, deadline: deadline, cancellationToken: context.CancellationToken);
        Console.WriteLine($"OK {item.Id} {item.Name} {item.Value} {item.CreatedAt.ToDateTime():O}");
    }
}
