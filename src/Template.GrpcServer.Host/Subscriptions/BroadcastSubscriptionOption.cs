namespace Template.GrpcServer.Host.Subscriptions;

public sealed class BroadcastSubscriptionOption
{
    [Range(1, 100_000)]
    public int MaxSubscribers { get; set; }

    // 購読者ごとの配信キュー。溢れたら古いものから捨てる
    [Range(1, 100_000)]
    public int QueueCapacity { get; set; }
}
