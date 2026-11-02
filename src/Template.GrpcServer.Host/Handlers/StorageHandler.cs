namespace Template.GrpcServer.Host.Handlers;

using System.IO.Pipelines;

using Google.Protobuf;

using Grpc.Core;

using Template.GrpcServer.Infrastructure.Storage;

public sealed class StorageHandler : Storage.StorageBase
{
    private readonly ILogger<StorageHandler> log;

    private readonly StorageHandlerOption option;

    private readonly IStorage storage;

    public StorageHandler(
        ILogger<StorageHandler> log,
        StorageHandlerOption option,
        IStorage storage)
    {
        this.log = log;
        this.option = option;
        this.storage = storage;
    }

    //--------------------------------------------------------------------------------
    // Client streaming
    //--------------------------------------------------------------------------------

    // 最初の1通がメタデータ、以降がチャンク。受信しながらPipe経由でストレージへ書く
    public override async Task<UploadReply> Upload(IAsyncStreamReader<UploadRequest> requestStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken) || (requestStream.Current.PayloadCase != UploadRequest.PayloadOneofCase.Metadata))
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", [("metadata", "First message must be metadata.")]);
        }

        var metadata = requestStream.Current.Metadata;
        var pipe = new Pipe();
        var writing = WriteAsync(metadata.Name, pipe, context.CancellationToken);
        var size = 0L;
        try
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                if (requestStream.Current.PayloadCase != UploadRequest.PayloadOneofCase.Chunk)
                {
                    throw RpcErrors.InvalidArgument("Request is invalid.", [("chunk", "Only chunks may follow metadata.")]);
                }

                var chunk = requestStream.Current.Chunk.Memory;
                var result = await pipe.Writer.WriteAsync(chunk, context.CancellationToken);
                if (result.IsCompleted)
                {
                    // 書込側が失敗して読取を終えている。残りは受け取らず、失敗は下のawaitで伝える
                    break;
                }

                size += chunk.Length;
            }

            await pipe.Writer.CompleteAsync();
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            await pipe.Writer.CompleteAsync(ex);
            throw;
        }
#pragma warning restore CA1031

        await writing;

        // grpc-encodingは要求メタデータから除外されるので、HttpContextから読む
        log.InfoFileUploaded(metadata.Name, size, context.GetHttpContext().Request.Headers["grpc-encoding"]);
        return new UploadReply { Name = metadata.Name, Size = size };
    }

    private async Task WriteAsync(string name, Pipe pipe, CancellationToken cancellationToken)
    {
        try
        {
            await storage.WriteAsync(name, pipe.Reader.AsStream(), cancellationToken);
            await pipe.Reader.CompleteAsync();
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            // 受信側のWriteAsyncをIsCompletedで抜けさせる(抜けさせないと背圧で止まる)
            await pipe.Reader.CompleteAsync(ex);
            throw;
        }
#pragma warning restore CA1031
    }

    //--------------------------------------------------------------------------------
    // Server streaming
    //--------------------------------------------------------------------------------

    // 最初の1通に総サイズを載せ、進捗はクライアントが受信量から算出する
    public override async Task Download(DownloadRequest request, IServerStreamWriter<DownloadReply> responseStream, ServerCallContext context)
    {
        if (!await storage.FileExistsAsync(request.Name, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "File not found."));
        }

        await using var stream = await storage.ReadAsync(request.Name, context.CancellationToken);
        await responseStream.WriteAsync(new DownloadReply { Metadata = new FileMetadata { Name = request.Name, Size = stream.Length } }, context.CancellationToken);

        var buffer = new byte[option.ChunkSize];
        int read;
        while ((read = await stream.ReadAsync(buffer, context.CancellationToken)) > 0)
        {
            await responseStream.WriteAsync(new DownloadReply { Chunk = ByteString.CopyFrom(buffer, 0, read) }, context.CancellationToken);
        }

        log.InfoFileDownloaded(request.Name, stream.Length, context.GetHttpContext().Response.Headers["grpc-encoding"]);
    }
}
