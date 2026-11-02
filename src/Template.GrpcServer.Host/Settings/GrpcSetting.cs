namespace Template.GrpcServer.Host.Settings;

using System.IO.Compression;

#pragma warning disable CA1034
public sealed class GrpcSetting
{
    [Required]
    public CompressionSetting Compression { get; set; } = default!;

    public sealed class CompressionSetting
    {
        public bool Enable { get; set; }

        public CompressionLevel Level { get; set; }
    }
}
#pragma warning restore CA1034
