namespace Template.GrpcServer.TestClient.Commands;

using Smart.CommandLine.Hosting;

// Client streaming。標準入力の各行("name value")を送る
[Command("upload", "Upload data from stdin (name value per line)")]
public sealed class UploadCommand : ICommandHandler
{
    private readonly Data.DataClient client;

    public UploadCommand(Data.DataClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var call = client.UploadData(cancellationToken: context.CancellationToken);
        while (await Console.In.ReadLineAsync(context.CancellationToken) is { } line)
        {
            var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if ((parts.Length != 2) || !Int32.TryParse(parts[1], CultureInfo.InvariantCulture, out var value))
            {
                Console.WriteLine($"NG: Invalid line. line=[{line}]");
                context.ExitCode = -1;
                return;
            }

            await call.RequestStream.WriteAsync(new SetDataRequest { Name = parts[0], Value = value }, context.CancellationToken);
        }

        await call.RequestStream.CompleteAsync();

        var reply = await call;
        Console.WriteLine($"OK {reply.Count}");
        foreach (var failure in reply.Failures)
        {
            Console.WriteLine($"  NG [{failure.Index}] name=[{failure.Name}] {failure.Reason}");
        }
    }
}
