namespace Template.GrpcServer.TestClient.Commands;

using Google.Protobuf;

using Grpc.Core;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Settings;

// Client streaming(メタデータ → チャンク)。--compressで要求ストリームをgzip圧縮する
[Command("upload-file", "Upload file")]
public sealed class UploadFileCommand : ICommandHandler
{
    private readonly ClientSetting setting;

    private readonly Storage.StorageClient client;

    [Option<string>("--file", "-f", Description = "file path", Required = true)]
    public string File { get; set; } = default!;

    [Option<string>("--name", "-n", Description = "name on the server (default: file name)")]
    public string? Name { get; set; }

    [Option<bool>("--compress", "-c", Description = "compress request (gzip)")]
    public bool Compress { get; set; }

    public UploadFileCommand(ClientSetting setting, Storage.StorageClient client)
    {
        this.setting = setting;
        this.client = client;
    }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var headers = Compress ? new Metadata { { "grpc-internal-encoding-request", "gzip" } } : null;
        using var call = client.Upload(headers, cancellationToken: context.CancellationToken);

        try
        {
            await using var stream = System.IO.File.OpenRead(File);
            await call.RequestStream.WriteAsync(new UploadRequest { Metadata = new FileMetadata { Name = Name ?? Path.GetFileName(File), Size = stream.Length } }, context.CancellationToken);

            var buffer = new byte[setting.ChunkSize];
            var sent = 0L;
            int read;
            while ((read = await stream.ReadAsync(buffer, context.CancellationToken)) > 0)
            {
                await call.RequestStream.WriteAsync(new UploadRequest { Chunk = ByteString.CopyFrom(buffer, 0, read) }, context.CancellationToken);
                sent += read;
                Progress.Write(sent, stream.Length);
            }

            await call.RequestStream.CompleteAsync();
        }
        catch (RpcException)
        {
            // サーバーが先に終了した(不正なパス等)。実際の状態は応答側で受け取る
        }

        var reply = await call;
        Console.WriteLine($"OK {reply.Name} {reply.Size}");
    }
}
