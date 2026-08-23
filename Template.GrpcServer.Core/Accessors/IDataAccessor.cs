namespace Template.GrpcServer.Accessors;

using Template.GrpcServer.Models;

[DataAccessor]
public interface IDataAccessor
{
    [Execute]
    void Create();

    [Query]
    ValueTask<List<DataEntity>> QueryListAsync(string? name);

    [QueryFirstOrDefault]
    ValueTask<DataEntity?> QueryAsync(long id);

    [ExecuteScalar]
    ValueTask<long> InsertAsync(string name, int value, DateTime createdAt);

    [Execute]
    ValueTask<int> DeleteAsync(long id);
}
