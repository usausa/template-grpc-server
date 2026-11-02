namespace Template.GrpcServer.Services;

public readonly record struct DataUpdateResult(DataWriteStatus Status, DataEntity? Entity);
