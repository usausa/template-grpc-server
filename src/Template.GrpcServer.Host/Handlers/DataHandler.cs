namespace Template.GrpcServer.Host.Handlers;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Template.GrpcServer.Host.Subscriptions;

public sealed class DataHandler : Data.DataBase
{
    private readonly DataService dataService;

    private readonly BroadcastSubscription subscription;

    public DataHandler(
        DataService dataService,
        BroadcastSubscription subscription)
    {
        this.dataService = dataService;
        this.subscription = subscription;
    }

    //--------------------------------------------------------------------------------
    // Unary
    //--------------------------------------------------------------------------------

    public override async Task<DataItem> GetData(GetDataRequest request, ServerCallContext context)
    {
        var entity = await dataService.QueryAsync(request.Id);
        if (entity is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Data not found."));
        }

        return MapToItem(entity);
    }

    public override async Task<SetDataReply> SetData(SetDataRequest request, ServerCallContext context)
    {
        var violations = ValidateSetRequest(request);
        if (violations.Count > 0)
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", violations);
        }

        var entity = new DataEntity { Name = request.Name, Value = request.Value };
        if (await dataService.InsertAsync(entity) == DataWriteStatus.Duplicate)
        {
            throw RpcErrors.AlreadyExists("Duplicate name.", "DUPLICATE_NAME", "name", request.Name);
        }

        return new SetDataReply { Id = entity.Id };
    }

    public override async Task<DeleteDataReply> DeleteData(DeleteDataRequest request, ServerCallContext context)
    {
        var status = await dataService.DeleteAsync(request.Id);
        return new DeleteDataReply { Deleted = status == DataWriteStatus.Success };
    }

    //--------------------------------------------------------------------------------
    // Server streaming
    //--------------------------------------------------------------------------------

    public override async Task ListData(ListDataRequest request, IServerStreamWriter<DataItem> responseStream, ServerCallContext context)
    {
        var sort = RequestHelper.Parse(request.Sort, DataSort.Id);
        await foreach (var entity in dataService.QueryListAsync(request.Name, sort, request.Desc, context.CancellationToken))
        {
            await responseStream.WriteAsync(MapToItem(entity), context.CancellationToken);
        }
    }

    //--------------------------------------------------------------------------------
    // Client streaming
    //--------------------------------------------------------------------------------

    // 1 件の失敗でストリーム全体を止めず、失敗した項目を位置と理由付きで返す
    public override async Task<UploadDataReply> UploadData(IAsyncStreamReader<SetDataRequest> requestStream, ServerCallContext context)
    {
        var reply = new UploadDataReply();
        var index = 0;
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var violations = ValidateSetRequest(request);
            if (violations.Count > 0)
            {
                reply.Failures.Add(new UploadFailure { Index = index, Name = request.Name, Reason = String.Join(' ', violations.Select(static x => x.Description)) });
            }
            else if (await dataService.InsertAsync(new DataEntity { Name = request.Name, Value = request.Value }) == DataWriteStatus.Success)
            {
                reply.Count++;
            }
            else
            {
                reply.Failures.Add(new UploadFailure { Index = index, Name = request.Name, Reason = "Duplicate name." });
            }

            index++;
        }

        return reply;
    }

    //--------------------------------------------------------------------------------
    // Bidirectional streaming
    //--------------------------------------------------------------------------------

    public override async Task Chat(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(new ChatMessage { Name = "server", Message = $"echo: {message.Message}" }, context.CancellationToken);
        }
    }

    //--------------------------------------------------------------------------------
    // Bidirectional streaming (server driven)
    //--------------------------------------------------------------------------------

    public override async Task Notify(IAsyncStreamReader<ClientMessage> requestStream, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
    {
        var subscriber = subscription.Subscribe(context.Peer);
        if (subscriber is null)
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted, "Too many subscribers."));
        }

        // 受信と配信を分け、応答ストリームへ書くのは配信側だけにする。どちらかが終われば他方も止める
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        try
        {
            await Task.WhenAll(
                ReceiveAsync(subscriber, requestStream, cts),
                SendAsync(subscriber, responseStream, cts));
        }
        finally
        {
            subscription.Unsubscribe(subscriber);
        }
    }

    private async Task ReceiveAsync(Subscriber subscriber, IAsyncStreamReader<ClientMessage> requestStream, CancellationTokenSource cts)
    {
        try
        {
            await foreach (var message in requestStream.ReadAllAsync(cts.Token))
            {
                switch (message.PayloadCase)
                {
                    case ClientMessage.PayloadOneofCase.Join:
                        subscriber.Name = message.Join.Name;
                        break;
                    case ClientMessage.PayloadOneofCase.Broadcast:
                        subscription.Broadcast(new ServerMessage { Broadcast = new Broadcast { Name = subscriber.Name, Message = message.Broadcast.Message } });
                        break;
                }
            }
        }
        finally
        {
            // クライアントが送信を終えたら配信も終える
            subscriber.Complete();
        }
    }

    private static async Task SendAsync(Subscriber subscriber, IServerStreamWriter<ServerMessage> responseStream, CancellationTokenSource cts)
    {
        try
        {
            await foreach (var message in subscriber.Reader.ReadAllAsync(cts.Token))
            {
                await responseStream.WriteAsync(message, cts.Token);
            }
        }
        finally
        {
            // 書込に失敗したら受信も止める
            await cts.CancelAsync();
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static List<(string Field, string Description)> ValidateSetRequest(SetDataRequest request)
    {
        var violations = new List<(string Field, string Description)>();
        if (String.IsNullOrEmpty(request.Name) || (request.Name.Length > 50))
        {
            violations.Add(("name", "Name is required and must be 50 characters or less."));
        }

        if (request.Value is < 0 or > 1_000_000)
        {
            violations.Add(("value", "Value must be between 0 and 1000000."));
        }

        return violations;
    }

    private static DataItem MapToItem(DataEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Value = entity.Value,
        CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc))
    };
}
