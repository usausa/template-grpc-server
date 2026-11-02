namespace Template.GrpcServer.Models.Entity;

using Template.GrpcServer.Infrastructure.Data;

public sealed class DataEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public int Value { get; set; }

    public DataStatus Status { get; set; }

    [TypeHandler(typeof(LabelsConverter))]
    public IReadOnlyDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }
}
