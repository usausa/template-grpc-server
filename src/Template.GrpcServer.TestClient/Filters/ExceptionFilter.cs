namespace Template.GrpcServer.TestClient.Filters;

using Google.Rpc;

using Grpc.Core;

using Smart.CommandLine.Hosting;

public sealed class ExceptionFilter : ICommandFilter
{
    private readonly ILogger<ExceptionFilter> log;

    public ExceptionFilter(ILogger<ExceptionFilter> log)
    {
        this.log = log;
    }

    public async ValueTask ExecuteAsync(CommandContext context, CommandDelegate next)
    {
#pragma warning disable CA1031
        try
        {
            await next(context);
        }
        catch (RpcException ex)
        {
            // google.rpc.Statusのdetails(フィールド違反 / 理由)とtrailerのtrace-idを表示する
            Console.WriteLine($"NG: {ex.StatusCode} {ex.Status.Detail}");
            var status = ex.GetRpcStatus();
            if (status is not null)
            {
                foreach (var violation in status.GetDetail<BadRequest>()?.FieldViolations ?? [])
                {
                    Console.WriteLine($"  {violation.Field}: {violation.Description}");
                }

                var info = status.GetDetail<ErrorInfo>();
                if (info is not null)
                {
                    Console.WriteLine($"  reason={info.Reason} domain={info.Domain} {String.Join(" ", info.Metadata.Select(static x => $"{x.Key}={x.Value}"))}");
                }

                foreach (var resource in status.Details.Where(static x => x.Is(ResourceInfo.Descriptor)).Select(static x => x.Unpack<ResourceInfo>()))
                {
                    Console.WriteLine($"  resource={resource.ResourceType}/{resource.ResourceName}");
                }
            }

            var traceId = ex.Trailers.GetValue("trace-id");
            if (traceId is not null)
            {
                Console.WriteLine($"  trace-id={traceId}");
            }

            context.ExitCode = -1;
        }
        catch (Exception ex)
        {
            log.ErrorUnknownException(ex, context.CommandType.Name);

            context.ExitCode = -1;
        }
#pragma warning restore CA1031
    }
}
