namespace Template.GrpcServer.Models.Parameters;

public sealed record DataUpdate(string? Name, int? Value, DataStatus? Status, IReadOnlyDictionary<string, string>? Labels);
