namespace Template.GrpcServer.Services;

using Template.GrpcServer.Accessors;
using Template.GrpcServer.Infrastructure.Data;

public sealed class DataService
{
    private readonly IDialect dialect;

    private readonly DataAccessor dataAccessor;

    private readonly ServiceContextProvider contextProvider;

    public DataService(
        IDialect dialect,
        DataAccessor dataAccessor,
        ServiceContextProvider contextProvider)
    {
        this.dialect = dialect;
        this.dataAccessor = dataAccessor;
        this.contextProvider = contextProvider;
    }

    public IAsyncEnumerable<DataEntity> QueryListAsync(DataFilter filter, DataSort sort, bool desc, CancellationToken cancellationToken = default) =>
        dataAccessor.QueryListAsync(dialect.Match(filter.Name), filter.Status, filter.CreatedFrom, filter.CreatedTo, sort, desc, cancellationToken);

    public async ValueTask<CursorPage<DataEntity>> QueryPageAsync(DataFilter filter, DataSort sort, bool desc, DataCursor? after, int size, CancellationToken cancellationToken = default)
    {
        var pattern = dialect.Match(filter.Name);
        var total = await dataAccessor.CountAsync(pattern, filter.Status, filter.CreatedFrom, filter.CreatedTo, cancellationToken);
        var items = await dataAccessor.QueryPageAsync(pattern, filter.Status, filter.CreatedFrom, filter.CreatedTo, sort, desc, after is null ? null : GetSortKey(after, sort), after?.Id, size + 1, cancellationToken);
        var hasNext = items.Count > size;
        if (hasNext)
        {
            items.RemoveAt(size);
        }

        return new CursorPage<DataEntity>(total, hasNext, items);
    }

    public ValueTask<List<DataEntity>> QueryByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default) =>
        dataAccessor.QueryByIdsAsync(ids, cancellationToken);

    public ValueTask<DataEntity?> QueryAsync(long id, CancellationToken cancellationToken = default) =>
        dataAccessor.QueryAsync(id, cancellationToken);

    public async ValueTask<DataWriteStatus> InsertAsync(DataEntity entity, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;

        try
        {
            entity.CreatedAt = context.Now.UtcDateTime;
            entity.Id = await dataAccessor.InsertAsync(entity.Name, entity.Value, entity.Status, LabelsConverter.ToDb(entity.Labels), entity.CreatedAt, cancellationToken);
            return DataWriteStatus.Success;
        }
        catch (DbException ex) when (dialect.IsDuplicate(ex))
        {
            return DataWriteStatus.Duplicate;
        }
    }

    public async ValueTask<DataWriteStatus> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var rows = await dataAccessor.DeleteAsync(id, cancellationToken);
        return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
    }

    private static object GetSortKey(DataCursor cursor, DataSort sort) => sort switch
    {
        DataSort.Name => cursor.Name,
        DataSort.Value => cursor.Value,
        DataSort.CreatedAt => cursor.CreatedAt,
        _ => cursor.Id
    };
}
