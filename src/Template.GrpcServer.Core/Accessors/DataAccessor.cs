namespace Template.GrpcServer.Accessors;

[DataAccessor]
public sealed partial class DataAccessor
{
    // 一覧はListに溜めず、readerから1件ずつ流す(Server streamingがそのまま送る)
    // ReSharper disable once CSharpWarnings::CS8424 (SDA0305がEnumeratorCancellation注釈を要求するため。実装はジェネレータ側で処理される)
    [Query]
    public partial IAsyncEnumerable<DataEntity> QueryListAsync(string? name, DataSort sort, bool desc, [EnumeratorCancellation] CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<DataEntity?> QueryAsync(long id);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, int value, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long id);
}
