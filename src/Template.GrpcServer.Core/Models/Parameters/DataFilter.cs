namespace Template.GrpcServer.Models.Parameters;

public sealed record DataFilter(string? Name, DataStatus? Status, DateTime? CreatedFrom, DateTime? CreatedTo);
