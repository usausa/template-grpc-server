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

    public ValueTask<DataEntity?> QueryAsync(long id) =>
        dataAccessor.QueryAsync(id);

    public async ValueTask<DataWriteStatus> InsertAsync(DataEntity entity)
    {
        var context = contextProvider.Current;

        try
        {
            entity.CreatedAt = context.Now.UtcDateTime;
            entity.Id = await dataAccessor.InsertAsync(entity.Name, entity.Value, entity.CreatedAt);
            return DataWriteStatus.Success;
        }
        catch (DbException ex) when (dialect.IsDuplicate(ex))
        {
            return DataWriteStatus.Duplicate;
        }
    }

    public async ValueTask<DataWriteStatus> DeleteAsync(long id)
    {
        var rows = await dataAccessor.DeleteAsync(id);
        return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
    }
}
