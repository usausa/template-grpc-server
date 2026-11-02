namespace Template.GrpcServer.Infrastructure.Data;

using System.Text.Json;
using System.Text.Json.Serialization;

using Smart.Data.Accessor.Converters;

public sealed partial class LabelsConverter : IValueConverter<string, IReadOnlyDictionary<string, string>>
{
    public static IReadOnlyDictionary<string, string> FromDb(string dbValue) =>
        JsonSerializer.Deserialize(dbValue, LabelsJsonContext.Default.DictionaryStringString) ?? [];

    public static string ToDb(IReadOnlyDictionary<string, string> clrValue) =>
        JsonSerializer.Serialize(clrValue, LabelsJsonContext.Default.IReadOnlyDictionaryStringString);

    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
    private sealed partial class LabelsJsonContext : JsonSerializerContext;
}
