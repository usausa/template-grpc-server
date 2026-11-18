namespace Template.GrpcServer.Host.Application.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

using Grpc.Core;

public sealed class ApplicationInstrument : IDisposable
{
    private readonly TimeProvider timeProvider;

    private readonly long startTimestamp;

    private readonly Meter meter;

    private readonly Counter<long> requestLongExecution;

    private readonly Counter<long> requestExecution;

    public ActivitySource ActivitySource { get; }

    public ApplicationInstrument(TimeProvider timeProvider, IMeterFactory meterFactory)
    {
        this.timeProvider = timeProvider;
        startTimestamp = timeProvider.GetTimestamp();

        ActivitySource = new ActivitySource(Source.Name, Source.Version);
        meter = meterFactory.Create(Source.Name, Source.Version);

        meter.CreateObservableCounter("application.uptime", ObserveApplicationUptime);

        requestLongExecution = meter.CreateCounter<long>("grpc.request.long.execution", description: "Long execution count.");
        requestExecution = meter.CreateCounter<long>("grpc.request.execution", description: "gRPC request count");
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        meter.Dispose();
    }

    private long ObserveApplicationUptime() =>
        (long)timeProvider.GetElapsedTime(startTimestamp).TotalSeconds;

    public void IncrementRequestExecution(string method, StatusCode status) =>
        requestExecution.Add(1, new("rpc.method", method), new("rpc.grpc.status_code", (int)status));

    public void IncrementRequestLongExecution(string method, StatusCode status) =>
        requestLongExecution.Add(1, new("rpc.method", method), new("rpc.grpc.status_code", (int)status));
}
