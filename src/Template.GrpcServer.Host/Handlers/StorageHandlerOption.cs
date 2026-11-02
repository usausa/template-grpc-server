namespace Template.GrpcServer.Host.Handlers;

public sealed class StorageHandlerOption
{
    // ファイル転送のチャンク。MaxReceiveMessageSize(既定4MB)より十分小さくする
    [Range(1024, 1_048_576)]
    public int ChunkSize { get; set; }
}
