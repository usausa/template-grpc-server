namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Unary
[Command("delete", "Delete data")]
public sealed class DeleteCommand : ICommandHandler
{
    private readonly TimeProvider timeProvider;

    private readonly ClientSetting setting;

    private readonly Data.DataClient client;

    [Option<long>("--id", "-i", Description = "id", Required = true)]
    public long Id { get; set; }

    public DeleteCommand(TimeProvider timeProvider, ClientSetting setting, Data.DataClient client)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var deadline = timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(setting.Deadline);
        var reply = await client.DeleteDataAsync(new DeleteDataRequest { Id = Id }, deadline: deadline, cancellationToken: context.CancellationToken);
        Console.WriteLine(reply.Deleted ? "OK" : "NG: Not found.");
        if (!reply.Deleted)
        {
            context.ExitCode = -1;
        }
    }
}
