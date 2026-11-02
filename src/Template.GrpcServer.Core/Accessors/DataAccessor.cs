namespace Template.GrpcServer.Accessors;

[DataAccessor]
public sealed partial class DataAccessor
{
    // 一覧はListに溜めず、readerから1件ずつ流す(Server streamingがそのまま送る)
    // ReSharper disable once CSharpWarnings::CS8424 (SDA0305がEnumeratorCancellation注釈を要求するため。実装はジェネレータ側で処理される)
    [Query]
    public partial IAsyncEnumerable<DataEntity> QueryListAsync(string? name, DataStatus? status, DateTime? createdFrom, DateTime? createdTo, DataSort sort, bool desc, [EnumeratorCancellation] CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? name, DataStatus? status, DateTime? createdFrom, DateTime? createdTo, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryPageAsync(string? name, DataStatus? status, DateTime? createdFrom, DateTime? createdTo, DataSort sort, bool desc, object? afterKey, long? afterId, int limit, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<DataEntity?> QueryAsync(long id, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, int value, DataStatus status, string labels, DateTime createdAt, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long id, CancellationToken cancellationToken);
}
