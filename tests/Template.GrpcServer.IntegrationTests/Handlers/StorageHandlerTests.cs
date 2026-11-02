namespace Template.GrpcServer.Handlers;

using System.Security.Cryptography;

using Google.Protobuf;

using Grpc.Core;

using Template.GrpcServer.Host;

public sealed class StorageHandlerTests : IClassFixture<TestApplicationFactory>
{
    private const int ChunkSize = 65536;

    private readonly TestApplicationFactory factory;

    public StorageHandlerTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task UploadThenDownloadReturnsSameBytes()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Storage.StorageClient(factory.CreateAuthorizedInvoker(channel));
        var data = new byte[(ChunkSize * 3) + 100];
        RandomNumberGenerator.Fill(data);
        var name = $"file-{Guid.NewGuid():N}.bin";

        // Act
        using var upload = client.Upload(cancellationToken: TestContext.Current.CancellationToken);
        await upload.RequestStream.WriteAsync(new UploadRequest { Metadata = new FileMetadata { Name = name, Size = data.Length } }, TestContext.Current.CancellationToken);
        for (var offset = 0; offset < data.Length; offset += ChunkSize)
        {
            await upload.RequestStream.WriteAsync(new UploadRequest { Chunk = ByteString.CopyFrom(data, offset, Math.Min(ChunkSize, data.Length - offset)) }, TestContext.Current.CancellationToken);
        }
        await upload.RequestStream.CompleteAsync();
        var uploaded = await upload;

        using var download = client.Download(new DownloadRequest { Name = name }, cancellationToken: TestContext.Current.CancellationToken);
        FileMetadata? metadata = null;
        using var buffer = new MemoryStream();
        await foreach (var reply in download.ResponseStream.ReadAllAsync(TestContext.Current.CancellationToken))
        {
            if (reply.PayloadCase == DownloadReply.PayloadOneofCase.Metadata)
            {
                metadata = reply.Metadata;
            }
            else
            {
                reply.Chunk.WriteTo(buffer);
            }
        }

        // Assert
        Assert.Equal(data.Length, uploaded.Size);
        Assert.NotNull(metadata);
        Assert.Equal(data.Length, metadata.Size);
        Assert.Equal(data, buffer.ToArray());
    }

    [Fact]
    public async Task UploadWithInvalidPathReturnsInvalidArgument()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Storage.StorageClient(factory.CreateAuthorizedInvoker(channel));

        // Act
        using var upload = client.Upload(cancellationToken: TestContext.Current.CancellationToken);
        await upload.RequestStream.WriteAsync(new UploadRequest { Metadata = new FileMetadata { Name = "../evil.bin", Size = 0 } }, TestContext.Current.CancellationToken);
        await upload.RequestStream.CompleteAsync();
        var exception = await Assert.ThrowsAsync<RpcException>(() => upload.ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task DownloadWithUnknownNameReturnsNotFound()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Storage.StorageClient(factory.CreateAuthorizedInvoker(channel));

        // Act
        using var download = client.Download(new DownloadRequest { Name = $"missing-{Guid.NewGuid():N}.bin" }, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<RpcException>(async () => await download.ResponseStream.MoveNext(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }
}
