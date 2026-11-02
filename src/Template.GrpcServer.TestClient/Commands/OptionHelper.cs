namespace Template.GrpcServer.TestClient.Commands;

using Google.Protobuf.WellKnownTypes;

public static class OptionHelper
{
    public static Timestamp? ParseTimestamp(string? value) =>
        String.IsNullOrEmpty(value) ? null : Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));

    public static Dictionary<string, string> ParseLabels(string? value)
    {
        var labels = new Dictionary<string, string>();
        foreach (var label in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = label.Split('=', 2);
            labels[pair[0]] = pair.Length > 1 ? pair[1] : string.Empty;
        }

        return labels;
    }
}
