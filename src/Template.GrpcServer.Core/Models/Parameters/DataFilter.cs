namespace Template.GrpcServer.Models.Parameters;

public sealed record DataFilter(string? Name, DateTime? CreatedFrom, DateTime? CreatedTo);
