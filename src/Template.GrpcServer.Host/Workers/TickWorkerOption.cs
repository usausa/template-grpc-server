namespace Template.GrpcServer.Host.Workers;

public sealed class TickWorkerOption
{
    public bool Enable { get; set; }

    [Range(100, 3_600_000)]
    public int Interval { get; set; }
}
