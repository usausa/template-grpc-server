namespace Template.GrpcServer.Host.Api.Handlers;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

public sealed class DataHandler : Data.DataBase
{
    private readonly DataService dataService;

    public DataHandler(DataService dataService)
    {
        this.dataService = dataService;
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
        ValidateSetRequest(request);

        var id = await dataService.InsertAsync(request.Name, request.Value);
        if (!id.HasValue)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Duplicate name."));
        }

        return new SetDataReply { Id = id.Value };
    }

    public override async Task<DeleteDataReply> DeleteData(DeleteDataRequest request, ServerCallContext context)
    {
        var deleted = await dataService.DeleteAsync(request.Id);
        return new DeleteDataReply { Deleted = deleted };
    }

    //--------------------------------------------------------------------------------
    // Server streaming
    //--------------------------------------------------------------------------------

    public override async Task ListData(ListDataRequest request, IServerStreamWriter<DataItem> responseStream, ServerCallContext context)
    {
        var list = await dataService.QueryListAsync(String.IsNullOrEmpty(request.Name) ? null : request.Name, context.CancellationToken);
        foreach (var entity in list)
        {
            await responseStream.WriteAsync(MapToItem(entity), context.CancellationToken);
        }
    }

    //--------------------------------------------------------------------------------
    // Client streaming
    //--------------------------------------------------------------------------------

    public override async Task<UploadDataReply> UploadData(IAsyncStreamReader<SetDataRequest> requestStream, ServerCallContext context)
    {
        var count = 0;
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            ValidateSetRequest(request);

            if ((await dataService.InsertAsync(request.Name, request.Value)).HasValue)
            {
                count++;
            }
        }

        return new UploadDataReply { Count = count };
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
    // Helper
    //--------------------------------------------------------------------------------

    private static void ValidateSetRequest(SetDataRequest request)
    {
        if (String.IsNullOrEmpty(request.Name) || (request.Name.Length > 50))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is invalid."));
        }
    }

    private static DataItem MapToItem(DataEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Value = entity.Value,
        CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc))
    };
}
