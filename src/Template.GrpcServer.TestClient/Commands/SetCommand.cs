namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Unary(検証エラーと重複はgoogle.rpc.Statusの詳細付きで返る)
[Command("set", "Set data")]
public sealed class SetCommand : ICommandHandler
{
    private readonly TimeProvider timeProvider;

    private readonly ClientSetting setting;

    private readonly Data.DataClient client;

    [Option<string>("--name", "-n", Description = "name", Required = true)]
    public string Name { get; set; } = default!;

    [Option<int>("--value", "-v", Description = "value", Required = true)]
    public int Value { get; set; }

    [Option<DataStatus>("--status", Description = "status")]
    public DataStatus Status { get; set; }

    [Option<string>("--labels", Description = "labels (k=v, comma separated)")]
    public string? Labels { get; set; }

    public SetCommand(TimeProvider timeProvider, ClientSetting setting, Data.DataClient client)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var request = new SetDataRequest { Name = Name, Value = Value, Status = Status };
        request.Labels.Add(OptionHelper.ParseLabels(Labels));
        var deadline = timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(setting.Deadline);
        var reply = await client.SetDataAsync(request, deadline: deadline, cancellationToken: context.CancellationToken);
        Console.WriteLine($"OK {reply.Id}");
    }
}
