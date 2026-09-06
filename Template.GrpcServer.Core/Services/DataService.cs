namespace Template.GrpcServer.Services;

using Template.GrpcServer.Accessors;
using Template.GrpcServer.Models;

public sealed class DataService
{
    private readonly IDialect dialect;

    private readonly IDataAccessor dataAccessor;

    private readonly TimeProvider timeProvider;

    public DataService(
        IDialect dialect,
        IAccessorResolver<IDataAccessor> dataAccessor,
        TimeProvider timeProvider)
    {
        this.dialect = dialect;
        this.dataAccessor = dataAccessor.Accessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        dataAccessor.Create();

    public ValueTask<List<DataEntity>> QueryListAsync(string? name, CancellationToken cancellationToken = default) =>
        dataAccessor.QueryListAsync(name, cancellationToken);

    public ValueTask<DataEntity?> QueryAsync(long id) =>
        dataAccessor.QueryAsync(id);

    public async ValueTask<long?> InsertAsync(string name, int value)
    {
        try
        {
            return await dataAccessor.InsertAsync(name, value, timeProvider.GetUtcNow().UtcDateTime).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            if (dialect.IsDuplicate(ex))
            {
                return null;
            }

            throw;
        }
    }

    public async ValueTask<bool> DeleteAsync(long id)
    {
        var rows = await dataAccessor.DeleteAsync(id).ConfigureAwait(false);
        return rows > 0;
    }
}
