namespace Template.GrpcServer.Host.Workers;

using Google.Protobuf.WellKnownTypes;

using Template.GrpcServer.Host.Subscriptions;

// サーバー主体の定期配信(ゲームループ相当)。Tick:Enableのときだけ登録される
public sealed class TickWorker : BackgroundService
{
    private readonly TimeProvider timeProvider;

    private readonly TickWorkerOption option;

    private readonly BroadcastSubscription subscription;

    public TickWorker(
        TimeProvider timeProvider,
        TickWorkerOption option,
        BroadcastSubscription subscription)
    {
        this.timeProvider = timeProvider;
        this.option = option;
        this.subscription = subscription;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(option.Interval), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (subscription.Count > 0)
            {
                subscription.Broadcast(new ServerMessage
                {
                    Tick = new Tick { Time = Timestamp.FromDateTimeOffset(timeProvider.GetUtcNow()), Subscribers = subscription.Count }
                });
            }
        }
    }
}
