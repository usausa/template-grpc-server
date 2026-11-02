namespace Template.GrpcServer.TestClient.Filters;

using Smart.CommandLine.Hosting;

public sealed class LoggingFilter : ICommandFilter
{
    private readonly ILogger<LoggingFilter> log;

    private readonly TimeProvider timeProvider;

    public LoggingFilter(ILogger<LoggingFilter> log, TimeProvider timeProvider)
    {
        this.log = log;
        this.timeProvider = timeProvider;
    }

    public async ValueTask ExecuteAsync(CommandContext context, CommandDelegate next)
    {
        log.InfoCommandStart(context.CommandType.Name);

        var timestamp = timeProvider.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = (long)timeProvider.GetElapsedTime(timestamp).TotalMilliseconds;
            log.InfoCommandEnd(context.CommandType.Name, elapsed);
        }
    }
}
