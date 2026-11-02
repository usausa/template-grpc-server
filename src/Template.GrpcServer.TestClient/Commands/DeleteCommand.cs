namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

// Unary
[Command("delete", "Delete data")]
public sealed class DeleteCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    [Option<long>("--id", "-i", Description = "id", Required = true)]
    public long Id { get; set; }

    public DeleteCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var reply = await client.DeleteDataAsync(new DeleteDataRequest { Id = Id }, cancellationToken: context.CancellationToken);
        Console.WriteLine(reply.Deleted ? "OK" : "NG: Not found.");
        if (!reply.Deleted)
        {
            context.ExitCode = -1;
        }
    }
}
