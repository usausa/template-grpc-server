namespace Template.GrpcServer.Host.Handlers;

using System.Buffers.Text;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Template.GrpcServer.Host.Subscriptions;

using EntityDataStatus = Template.GrpcServer.Models.Entity.DataStatus;

public sealed class DataHandler : Data.DataBase
{
    private const int DefaultPageSize = 20;

    private const int MaxPageSize = 100;

    private const int MaxBatchSize = 100;

    private const int MaxLabelCount = 16;

    private const int MaxLabelLength = 63;

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
        var entity = await dataService.QueryAsync(request.Id, context.CancellationToken);
        if (entity is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Data not found."));
        }

        return MapToItem(entity);
    }

    public override async Task<BatchGetDataReply> BatchGetData(BatchGetDataRequest request, ServerCallContext context)
    {
        if ((request.Ids.Count == 0) || (request.Ids.Count > MaxBatchSize))
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", [("ids", $"Ids must have 1 to {MaxBatchSize} items.")]);
        }

        var entities = (await dataService.QueryByIdsAsync(request.Ids.Distinct(), context.CancellationToken)).ToDictionary(static x => x.Id);
        var missing = request.Ids.Where(x => !entities.ContainsKey(x)).Distinct().ToList();
        if (missing.Count > 0)
        {
            throw RpcErrors.NotFound("Data not found.", "data", missing.Select(static x => x.ToString(CultureInfo.InvariantCulture)));
        }

        var reply = new BatchGetDataReply();
        reply.Items.AddRange(request.Ids.Select(x => MapToItem(entities[x])));
        return reply;
    }

    public override async Task<SetDataReply> SetData(SetDataRequest request, ServerCallContext context)
    {
        var violations = ValidateSetRequest(request);
        if (violations.Count > 0)
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", violations);
        }

        var entity = ToEntity(request);
        if (await dataService.InsertAsync(entity, context.CancellationToken) == DataWriteStatus.Duplicate)
        {
            throw RpcErrors.AlreadyExists("Duplicate name.", "DUPLICATE_NAME", "name", request.Name);
        }

        return new SetDataReply { Id = entity.Id };
    }

    public override async Task<DeleteDataReply> DeleteData(DeleteDataRequest request, ServerCallContext context)
    {
        var status = await dataService.DeleteAsync(request.Id, context.CancellationToken);
        return new DeleteDataReply { Deleted = status == DataWriteStatus.Success };
    }

    public override async Task<ListDataReply> ListData(ListDataRequest request, ServerCallContext context)
    {
        if (request.PageSize < 0)
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", [("page_size", "Page size must not be negative.")]);
        }

        if (!System.Enum.IsDefined(request.Status))
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", [("status", "Status is invalid.")]);
        }

        var sort = RequestHelper.Parse(request.Sort, DataSort.Id);
        DataCursor? after = null;
        if (!String.IsNullOrEmpty(request.PageToken))
        {
            var token = DecodePageToken(request.PageToken);
            if ((token is null) || !IsSameCondition(token, request, sort))
            {
                throw RpcErrors.InvalidArgument("Request is invalid.", [("page_token", "Page token is invalid or does not match the request.")]);
            }

            after = new DataCursor(token.LastId, token.LastName, token.LastValue, token.LastCreatedAt.ToDateTime());
        }

        var size = request.PageSize == 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var filter = new DataFilter(request.Name, ToFilterStatus(request.Status), request.CreatedFrom?.ToDateTime(), request.CreatedTo?.ToDateTime());
        var page = await dataService.QueryPageAsync(filter, sort, request.Desc, after, size, context.CancellationToken);

        var reply = new ListDataReply { TotalSize = page.Total };
        reply.Items.AddRange(page.Items.Select(MapToItem));
        if (page.HasNext)
        {
            reply.NextPageToken = EncodePageToken(request, sort, reply.Items[^1]);
        }

        return reply;
    }

    //--------------------------------------------------------------------------------
    // Server streaming
    //--------------------------------------------------------------------------------

    public override async Task ExportData(ExportDataRequest request, IServerStreamWriter<DataItem> responseStream, ServerCallContext context)
    {
        if (!System.Enum.IsDefined(request.Status))
        {
            throw RpcErrors.InvalidArgument("Request is invalid.", [("status", "Status is invalid.")]);
        }

        var sort = RequestHelper.Parse(request.Sort, DataSort.Id);
        var filter = new DataFilter(request.Name, ToFilterStatus(request.Status), request.CreatedFrom?.ToDateTime(), request.CreatedTo?.ToDateTime());
        await foreach (var entity in dataService.QueryListAsync(filter, sort, request.Desc, context.CancellationToken))
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
            else if (await dataService.InsertAsync(ToEntity(request), context.CancellationToken) == DataWriteStatus.Success)
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

        if (!System.Enum.IsDefined(request.Status))
        {
            violations.Add(("status", "Status is invalid."));
        }

        if ((request.Labels.Count > MaxLabelCount) || request.Labels.Any(static x => (x.Key.Length is 0 or > MaxLabelLength) || (x.Value.Length > MaxLabelLength)))
        {
            violations.Add(("labels", $"Labels must be {MaxLabelCount} items or less, with keys of 1 to {MaxLabelLength} characters and values of {MaxLabelLength} characters or less."));
        }

        return violations;
    }

    private static DataEntity ToEntity(SetDataRequest request) => new()
    {
        Name = request.Name,
        Value = request.Value,
        Status = request.Status == DataStatus.Unspecified ? EntityDataStatus.Active : (EntityDataStatus)(int)request.Status,
        Labels = new Dictionary<string, string>(request.Labels)
    };

    private static EntityDataStatus? ToFilterStatus(DataStatus status) =>
        status == DataStatus.Unspecified ? null : (EntityDataStatus)(int)status;

    private static string EncodePageToken(ListDataRequest request, DataSort sort, DataItem last) =>
        Base64Url.EncodeToString(new DataPageToken
        {
            Name = request.Name,
            Sort = sort.ToString(),
            Desc = request.Desc,
            CreatedFrom = request.CreatedFrom,
            CreatedTo = request.CreatedTo,
            Status = (int)request.Status,
            LastId = last.Id,
            LastName = last.Name,
            LastValue = last.Value,
            LastCreatedAt = last.CreatedAt
        }.ToByteArray());

    private static DataPageToken? DecodePageToken(string value)
    {
        try
        {
            var token = DataPageToken.Parser.ParseFrom(Base64Url.DecodeFromChars(value));
            return token.LastCreatedAt is null ? null : token;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (InvalidProtocolBufferException)
        {
            return null;
        }
    }

    private static bool IsSameCondition(DataPageToken token, ListDataRequest request, DataSort sort) =>
        (token.Name == request.Name) &&
        (token.Sort == sort.ToString()) &&
        (token.Desc == request.Desc) &&
        Equals(token.CreatedFrom, request.CreatedFrom) &&
        Equals(token.CreatedTo, request.CreatedTo) &&
        (token.Status == (int)request.Status);

    private static DataItem MapToItem(DataEntity entity)
    {
        var item = new DataItem
        {
            Id = entity.Id,
            Name = entity.Name,
            Value = entity.Value,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc)),
            Status = (DataStatus)(int)entity.Status
        };
        foreach (var label in entity.Labels)
        {
            item.Labels.Add(label.Key, label.Value);
        }

        return item;
    }
}
