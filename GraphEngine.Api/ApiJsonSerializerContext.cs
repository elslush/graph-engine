using System.Text.Json.Serialization;

namespace GraphEngine.Api;

[JsonSerializable(typeof(ExecuteGraphRequest))]
[JsonSerializable(typeof(ExecuteGraphResponse))]
[JsonSerializable(typeof(SerializeGraphRequest))]
[JsonSerializable(typeof(SerializeGraphResponse))]
internal partial class ApiJsonSerializerContext : JsonSerializerContext;
