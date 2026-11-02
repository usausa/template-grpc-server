namespace Template.GrpcServer.Application;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

using Grpc.Core;
using Grpc.Health.V1;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Template.GrpcServer.Host;
using Template.GrpcServer.Host.Application.Telemetry;

public sealed class PipelineTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public PipelineTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CallWithoutTokenReturnsUnauthenticated()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(channel);

        // Act
        using var call = client.GetDataAsync(new GetDataRequest { Id = 1 }, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task HealthCheckReturnsServing()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Health.HealthClient(channel);

        // Act
        var response = await client.CheckAsync(new HealthCheckRequest(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, response.Status);
    }

    [Fact]
    public async Task RequestIsCountedWithStatusCode()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var measurements = new ConcurrentQueue<(string Method, int Status)>();
        using var listener = StartListener(measurements);

        // Act
        using var call = client.GetDataAsync(new GetDataRequest { Id = Int64.MaxValue }, cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);

        // Assert
        Assert.Contains(("/data.v1.Data/GetData", (int)StatusCode.NotFound), measurements);
    }

    [Fact]
    public async Task DeadlineExceededIsCountedByServer()
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var measurements = new ConcurrentQueue<(string Method, int Status)>();
        using var listener = StartListener(measurements);
        await using var locker = new SqliteConnection($"Data Source={factory.DatabasePath}");
        await locker.OpenAsync(timeout.Token);
        await using var transaction = await locker.BeginTransactionAsync(timeout.Token);

        // Act
        using var call = client.SetDataAsync(new SetDataRequest { Name = $"deadline-{Guid.NewGuid():N}", Value = 1 }, deadline: DateTime.UtcNow.AddMilliseconds(300), cancellationToken: timeout.Token);
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
        while (!measurements.Contains(("/data.v1.Data/SetData", (int)StatusCode.DeadlineExceeded)))
        {
            await Task.Delay(50, timeout.Token);
        }

        // Assert
        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
    }

    private MeterListener StartListener(ConcurrentQueue<(string Method, int Status)> measurements)
    {
        var meterFactory = factory.Services.GetRequiredService<IMeterFactory>();
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (ReferenceEquals(instrument.Meter.Scope, meterFactory) && (instrument.Meter.Name == Source.Name) && (instrument.Name == "grpc.request.execution"))
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string? method = null;
            int? status = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "rpc.method")
                {
                    method = tag.Value as string;
                }
                else if (tag.Key == "rpc.grpc.status_code")
                {
                    status = tag.Value as int?;
                }
            }

            if ((method is not null) && status.HasValue)
            {
                measurements.Enqueue((method, status.Value));
            }
        });
        listener.Start();
        return listener;
    }
}
