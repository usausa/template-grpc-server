namespace Template.GrpcServer.TestClient.Commands;

public static class DataItemExtensions
{
    public static string ToDisplayString(this DataItem item) =>
        $"{item.Id} {item.Name} {item.Value} {item.CreatedAt.ToDateTime():O} {item.Status} [{String.Join(',', item.Labels.Select(static x => $"{x.Key}={x.Value}"))}]";
}
