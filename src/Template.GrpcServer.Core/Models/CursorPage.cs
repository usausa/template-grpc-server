namespace Template.GrpcServer.Models;

public sealed record CursorPage<T>(int Total, bool HasNext, IReadOnlyList<T> Items);
