namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Unary
[Command("list", "List data (paged)")]
public sealed class ListCommand : ICommandHandler
{
    private readonly TimeProvider timeProvider;

    private readonly ClientSetting setting;

    private readonly Data.DataClient client;

    [Option<string>("--name", "-n", Description = "name filter")]
    public string? Name { get; set; }

    [Option<string>("--sort", "-s", Description = "sort column (Id / Name / Value / CreatedAt)")]
    public string? Sort { get; set; }

    [Option<bool>("--desc", "-d", Description = "sort descending")]
    public bool Desc { get; set; }

    [Option<string>("--from", Description = "created at or after (ISO 8601)")]
    public string? From { get; set; }

    [Option<string>("--to", Description = "created before (ISO 8601)")]
    public string? To { get; set; }

    [Option<DataStatus>("--status", Description = "status filter")]
    public DataStatus Status { get; set; }

    [Option<int>("--size", Description = "page size (0 is the server default)")]
    public int Size { get; set; }

    [Option<string>("--token", "-t", Description = "page token")]
    public string? Token { get; set; }

    [Option<bool>("--all", "-a", Description = "follow page tokens to the end")]
    public bool All { get; set; }

    public ListCommand(TimeProvider timeProvider, ClientSetting setting, Data.DataClient client)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var request = new ListDataRequest
        {
            Name = Name ?? string.Empty,
            Sort = Sort ?? string.Empty,
            Desc = Desc,
            CreatedFrom = OptionHelper.ParseTimestamp(From),
            CreatedTo = OptionHelper.ParseTimestamp(To),
            Status = Status,
            PageSize = Size,
            PageToken = Token ?? string.Empty
        };

        var count = 0;
        while (true)
        {
            var deadline = timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(setting.Deadline);
            var reply = await client.ListDataAsync(request, deadline: deadline, cancellationToken: context.CancellationToken);
            foreach (var item in reply.Items)
            {
                Console.WriteLine(item.ToDisplayString());
            }

            count += reply.Items.Count;
            if (!All || String.IsNullOrEmpty(reply.NextPageToken))
            {
                Console.WriteLine($"OK {count} total={reply.TotalSize} next=[{reply.NextPageToken}]");
                return;
            }

            request.PageToken = reply.NextPageToken;
        }
    }
}
