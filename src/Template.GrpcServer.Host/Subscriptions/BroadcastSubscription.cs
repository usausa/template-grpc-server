namespace Template.GrpcServer.Host.Subscriptions;

using System.Collections.Concurrent;

// Notifyの購読者を保持し、サーバー主体で全員へ配信する。購読者ごとのキューに積み、応答ストリームへの書込は各接続のハンドラーだけが行う
public sealed class BroadcastSubscription
{
    private readonly BroadcastSubscriptionOption option;

    private readonly ConcurrentDictionary<string, Subscriber> subscribers = new();

    private int counter;

    public int Count => subscribers.Count;

    public BroadcastSubscription(BroadcastSubscriptionOption option)
    {
        this.option = option;
    }

    public Subscriber? Subscribe(string peer)
    {
        var subscriber = new Subscriber($"{Interlocked.Increment(ref counter)}:{peer}", option.QueueCapacity);
        subscribers[subscriber.Id] = subscriber;
        if (subscribers.Count > option.MaxSubscribers)
        {
            Unsubscribe(subscriber);
            return null;
        }

        return subscriber;
    }

    public void Unsubscribe(Subscriber subscriber)
    {
        subscribers.TryRemove(subscriber.Id, out _);
        subscriber.Complete();
    }

    // 遅い購読者の分は古いものから捨てるので、配信側は待たない
    public void Broadcast(ServerMessage message)
    {
        foreach (var subscriber in subscribers.Values)
        {
            subscriber.Enqueue(message);
        }
    }
}
