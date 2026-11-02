namespace Template.GrpcServer.Services;

using Template.GrpcServer.Accessors;

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

    public IAsyncEnumerable<DataEntity> QueryListAsync(string? name, DataSort sort, bool desc, CancellationToken cancellationToken = default) =>
        dataAccessor.QueryListAsync(dialect.Match(name), sort, desc, cancellationToken);

    public ValueTask<DataEntity?> QueryAsync(long id, CancellationToken cancellationToken = default) =>
        dataAccessor.QueryAsync(id, cancellationToken);

    public async ValueTask<DataWriteStatus> InsertAsync(DataEntity entity, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;

        try
        {
            entity.CreatedAt = context.Now.UtcDateTime;
            entity.Id = await dataAccessor.InsertAsync(entity.Name, entity.Value, entity.CreatedAt, cancellationToken);
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
}
