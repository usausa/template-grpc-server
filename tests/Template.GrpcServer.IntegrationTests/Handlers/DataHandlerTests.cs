namespace Template.GrpcServer.Handlers;

using Google.Rpc;

using Grpc.Core;

using Microsoft.Extensions.DependencyInjection;

using Template.GrpcServer.Host;
using Template.GrpcServer.Host.Subscriptions;

public sealed class DataHandlerTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public DataHandlerTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SetDataThenGetDataReturnsItem()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var name = $"item-{Guid.NewGuid():N}";

        // Act
        var reply = await client.SetDataAsync(new SetDataRequest { Name = name, Value = 10 }, cancellationToken: TestContext.Current.CancellationToken);
        var item = await client.GetDataAsync(new GetDataRequest { Id = reply.Id }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(reply.Id, item.Id);
        Assert.Equal(name, item.Name);
        Assert.Equal(10, item.Value);
    }

    [Fact]
    public async Task SetDataWithInvalidRequestReturnsFieldViolations()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));

        // Act
        using var call = client.SetDataAsync(new SetDataRequest { Name = string.Empty, Value = -1 }, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        var violations = exception.GetRpcStatus()?.GetDetail<BadRequest>()?.FieldViolations;
        Assert.NotNull(violations);
        Assert.Collection(
            violations,
            static x => Assert.Equal("name", x.Field),
            static x => Assert.Equal("value", x.Field));
        Assert.NotNull(exception.Trailers.GetValue("trace-id"));
    }

    [Fact]
    public async Task SetDataWithDuplicateNameReturnsErrorInfo()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var name = $"duplicate-{Guid.NewGuid():N}";
        await client.SetDataAsync(new SetDataRequest { Name = name, Value = 1 }, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        using var call = client.SetDataAsync(new SetDataRequest { Name = name, Value = 2 }, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.AlreadyExists, exception.StatusCode);
        var info = exception.GetRpcStatus()?.GetDetail<ErrorInfo>();
        Assert.NotNull(info);
        Assert.Equal("DUPLICATE_NAME", info.Reason);
        Assert.Equal(name, info.Metadata["name"]);
    }

    [Fact]
    public async Task GetDataWithUnknownIdReturnsNotFound()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));

        // Act
        using var call = client.GetDataAsync(new GetDataRequest { Id = Int64.MaxValue }, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task DeleteDataReturnsDeletedOnlyOnce()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var reply = await client.SetDataAsync(new SetDataRequest { Name = $"delete-{Guid.NewGuid():N}", Value = 1 }, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var first = await client.DeleteDataAsync(new DeleteDataRequest { Id = reply.Id }, cancellationToken: TestContext.Current.CancellationToken);
        var second = await client.DeleteDataAsync(new DeleteDataRequest { Id = reply.Id }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(first.Deleted);
        Assert.False(second.Deleted);
    }

    [Fact]
    public async Task ListDataReturnsItemsSortedByName()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var prefix = $"list-{Guid.NewGuid():N}";
        await client.SetDataAsync(new SetDataRequest { Name = $"{prefix}-b", Value = 1 }, cancellationToken: TestContext.Current.CancellationToken);
        await client.SetDataAsync(new SetDataRequest { Name = $"{prefix}-a", Value = 2 }, cancellationToken: TestContext.Current.CancellationToken);
        await client.SetDataAsync(new SetDataRequest { Name = $"{prefix}-c", Value = 3 }, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        using var call = client.ListData(new ListDataRequest { Name = prefix, Sort = "Name", Desc = true }, cancellationToken: TestContext.Current.CancellationToken);
        var names = new List<string>();
        await foreach (var item in call.ResponseStream.ReadAllAsync(TestContext.Current.CancellationToken))
        {
            names.Add(item.Name);
        }

        // Assert
        string[] expected = [$"{prefix}-c", $"{prefix}-b", $"{prefix}-a"];
        Assert.Equal(expected, names);
    }

    [Fact]
    public async Task UploadDataReturnsCountAndFailures()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var name = $"upload-{Guid.NewGuid():N}";

        // Act
        using var call = client.UploadData(cancellationToken: TestContext.Current.CancellationToken);
        await call.RequestStream.WriteAsync(new SetDataRequest { Name = $"{name}-1", Value = 1 }, TestContext.Current.CancellationToken);
        await call.RequestStream.WriteAsync(new SetDataRequest { Name = $"{name}-1", Value = 2 }, TestContext.Current.CancellationToken);
        await call.RequestStream.WriteAsync(new SetDataRequest { Name = $"{name}-2", Value = -1 }, TestContext.Current.CancellationToken);
        await call.RequestStream.CompleteAsync();
        var reply = await call;

        // Assert
        Assert.Equal(1, reply.Count);
        Assert.Collection(
            reply.Failures,
            static x => Assert.Equal(1, x.Index),
            static x => Assert.Equal(2, x.Index));
    }

    [Fact]
    public async Task ChatEchoesMessages()
    {
        // Arrange
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));

        // Act
        using var call = client.Chat(cancellationToken: TestContext.Current.CancellationToken);
        await call.RequestStream.WriteAsync(new ChatMessage { Name = "me", Message = "hello" }, TestContext.Current.CancellationToken);
        await call.RequestStream.CompleteAsync();
        var messages = new List<string>();
        await foreach (var message in call.ResponseStream.ReadAllAsync(TestContext.Current.CancellationToken))
        {
            messages.Add(message.Message);
        }

        // Assert
        Assert.Equal("echo: hello", Assert.Single(messages));
    }

    [Fact]
    public async Task NotifyBroadcastsToAllSubscribers()
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var channel = factory.CreateChannel();
        var client = new Data.DataClient(factory.CreateAuthorizedInvoker(channel));
        var subscription = factory.Services.GetRequiredService<BroadcastSubscription>();
        using var alice = client.Notify(cancellationToken: timeout.Token);
        using var bob = client.Notify(cancellationToken: timeout.Token);
        await alice.RequestStream.WriteAsync(new ClientMessage { Join = new JoinRequest { Name = "alice" } }, timeout.Token);
        await bob.RequestStream.WriteAsync(new ClientMessage { Join = new JoinRequest { Name = "bob" } }, timeout.Token);
        while (subscription.Count < 2)
        {
            await Task.Delay(10, timeout.Token);
        }

        // Act
        await alice.RequestStream.WriteAsync(new ClientMessage { Broadcast = new BroadcastRequest { Message = "hello" } }, timeout.Token);
        var aliceReceived = await ReadAsync(alice.ResponseStream, timeout.Token);
        var bobReceived = await ReadAsync(bob.ResponseStream, timeout.Token);
        await alice.RequestStream.CompleteAsync();
        await bob.RequestStream.CompleteAsync();

        // Assert
        Assert.Equal("alice", aliceReceived.Broadcast.Name);
        Assert.Equal("hello", aliceReceived.Broadcast.Message);
        Assert.Equal("alice", bobReceived.Broadcast.Name);
        Assert.Equal("hello", bobReceived.Broadcast.Message);
    }

    private static async Task<ServerMessage> ReadAsync(IAsyncStreamReader<ServerMessage> stream, CancellationToken cancellationToken)
    {
        Assert.True(await stream.MoveNext(cancellationToken));
        return stream.Current;
    }
}
