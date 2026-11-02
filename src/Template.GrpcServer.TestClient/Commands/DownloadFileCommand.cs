namespace Template.GrpcServer.TestClient.Commands;

using Grpc.Core;

using Smart.CommandLine.Hosting;

// Server streaming(メタデータ → チャンク)。進捗はメタデータの総サイズと受信量から算出する
[Command("download-file", "Download file")]
public sealed class DownloadFileCommand : ICommandHandler
{
    private readonly Storage.StorageClient client;

    [Option<string>("--name", "-n", Description = "name on the server", Required = true)]
    public string Name { get; set; } = default!;

    [Option<string>("--output", "-o", Description = "output path (default: name)")]
    public string? Output { get; set; }

    public DownloadFileCommand(Storage.StorageClient client)
    {
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var call = client.Download(new DownloadRequest { Name = Name }, cancellationToken: context.CancellationToken);

        var total = 0L;
        var received = 0L;
        await using var stream = File.Create(Output ?? Path.GetFileName(Name));
        await foreach (var reply in call.ResponseStream.ReadAllAsync(context.CancellationToken))
        {
            switch (reply.PayloadCase)
            {
                case DownloadReply.PayloadOneofCase.Metadata:
                    total = reply.Metadata.Size;
                    break;
                case DownloadReply.PayloadOneofCase.Chunk:
                    await stream.WriteAsync(reply.Chunk.Memory, context.CancellationToken);
                    received += reply.Chunk.Length;
                    Progress.Write(received, total);
                    break;
            }
        }

        Console.WriteLine($"OK {Name} {received}");
    }
}
