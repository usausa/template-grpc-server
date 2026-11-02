namespace Template.GrpcServer.Host.Application.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

using Grpc.Core;

public sealed class ApplicationInstrument : IDisposable
{
    private readonly Meter meter;

    public ActivitySource ActivitySource { get; }

    private readonly Counter<long> requestLongExecution;

    private readonly Counter<long> requestExecution;

    public ApplicationInstrument(IMeterFactory meterFactory)
    {
        ActivitySource = new ActivitySource(Source.Name, Source.Version);
        meter = meterFactory.Create(Source.Name, Source.Version);

        meter.CreateObservableCounter("application.uptime", ObserveApplicationUptime);

        requestLongExecution = meter.CreateCounter<long>("grpc.request.long.execution", description: "Long execution count.");
        requestExecution = meter.CreateCounter<long>("grpc.request.execution", description: "gRPC request count");
    }

    public void Dispose()
    {
        meter.Dispose();
    }

    private static long ObserveApplicationUptime() =>
        (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;

    public void IncrementRequestExecution(string method, StatusCode status) =>
        requestExecution.Add(1, new("rpc.method", method), new("rpc.grpc.status_code", (int)status));

    public void IncrementRequestLongExecution(string method, StatusCode status) =>
        requestLongExecution.Add(1, new("rpc.method", method), new("rpc.grpc.status_code", (int)status));
}
