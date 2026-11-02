namespace Template.GrpcServer.Host.Handlers;

internal static partial class Log
{
    // Storage

    [LoggerMessage(Level = LogLevel.Information, Message = "File uploaded. name=[{name}], size=[{size}], encoding=[{encoding}]")]
    public static partial void InfoFileUploaded(this ILogger logger, string name, long size, string? encoding);

    [LoggerMessage(Level = LogLevel.Information, Message = "File downloaded. name=[{name}], size=[{size}], encoding=[{encoding}]")]
    public static partial void InfoFileDownloaded(this ILogger logger, string name, long size, string? encoding);
}
