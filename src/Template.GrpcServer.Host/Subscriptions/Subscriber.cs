namespace Template.GrpcServer.Host.Subscriptions;

using System.Threading.Channels;

public sealed class Subscriber
{
    private readonly Channel<ServerMessage> channel;

    public string Id { get; }

    public string Name { get; set; } = string.Empty;

    public ChannelReader<ServerMessage> Reader => channel.Reader;

    public Subscriber(string id, int capacity)
    {
        Id = id;
        channel = Channel.CreateBounded<ServerMessage>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public void Enqueue(ServerMessage message) =>
        channel.Writer.TryWrite(message);

    public void Complete() =>
        channel.Writer.TryComplete();
}
