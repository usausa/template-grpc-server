namespace Template.GrpcServer.TestClient.Filters;

internal static partial class Log
{
    // Info

    [LoggerMessage(Level = LogLevel.Information, Message = "Command start. command=[{commandName}]")]
    public static partial void InfoCommandStart(this ILogger logger, string commandName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Command end. command=[{commandName}], elapsed=[{elapsed}]")]
    public static partial void InfoCommandEnd(this ILogger logger, string commandName, long elapsed);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception. command=[{commandName}]")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex, string commandName);
}
