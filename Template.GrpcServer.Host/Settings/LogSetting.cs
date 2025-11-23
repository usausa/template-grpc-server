namespace Template.GrpcServer.Host.Settings;

public sealed class LogSetting
{
    public sealed class DataEntry
    {
        public bool SqlTrace { get; set; }
    }

    public DataEntry? Data { get; set; }
}
