namespace Template.GrpcServer.TestClient.Commands;

using Google.Protobuf.WellKnownTypes;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Unary
[Command("update", "Update data")]
public sealed class UpdateCommand : ICommandHandler
{
    private readonly TimeProvider timeProvider;

    private readonly ClientSetting setting;

    private readonly Data.DataClient client;

    [Option<long>("--id", "-i", Description = "id", Required = true)]
    public long Id { get; set; }

    [Option<string>("--name", "-n", Description = "name")]
    public string? Name { get; set; }

    [Option<int>("--value", "-v", Description = "value")]
    public int Value { get; set; }

    [Option<DataStatus>("--status", Description = "status")]
    public DataStatus Status { get; set; }

    [Option<string>("--labels", Description = "labels (k=v, comma separated)")]
    public string? Labels { get; set; }

    [Option<string>("--mask", Description = "fields to update (name / value / status / labels or *, comma separated)")]
    public string? Mask { get; set; }

    [Option<string>("--etag", Description = "etag")]
    public string? Etag { get; set; }

    public UpdateCommand(TimeProvider timeProvider, ClientSetting setting, Data.DataClient client)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var data = new DataItem { Id = Id, Name = Name ?? string.Empty, Value = Value, Status = Status, Etag = Etag ?? string.Empty };
        data.Labels.Add(OptionHelper.ParseLabels(Labels));
        var request = new UpdateDataRequest { Data = data };
        if (Mask is not null)
        {
            request.UpdateMask = new FieldMask();
            request.UpdateMask.Paths.AddRange(Mask.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        var deadline = timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(setting.Deadline);
        var item = await client.UpdateDataAsync(request, deadline: deadline, cancellationToken: context.CancellationToken);
        Console.WriteLine($"OK {item.ToDisplayString()}");
    }
}
