using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphEngine.Core.Programmatic;

public class Edge
{
    public Guid Id { get; set; }
    public string Source { get; set; }
    public string SourceHandle { get; set; }
    public string Target { get; set; }
    public string TargetHandle { get; set; }
    public Dictionary<string, object?> Annotations { get; set; } = new();

    public Edge(
        Guid id,
        string source,
        string sourceHandle,
        string target,
        string targetHandle,
        Dictionary<string, object?>? annotations = null)
    {
        Id = id;
        Source = source;
        SourceHandle = sourceHandle;
        Target = target;
        TargetHandle = targetHandle;
        Annotations = annotations ?? new Dictionary<string, object?>();
    }

    public Edge(
        string source,
        string sourceHandle,
        string target,
        string targetHandle,
        Dictionary<string, object?>? annotations = null)
        : this(Guid.NewGuid(), source, sourceHandle, target, targetHandle, annotations)
    {
    }

    public string Serialize(JsonSerializerOptions? options = null)
    {
        var dto = new EdgeDto
        {
            Id = Id,
            Source = Source,
            SourceHandle = SourceHandle,
            Target = Target,
            TargetHandle = TargetHandle,
            // Only include annotations when non-empty to mirror TS behavior
            Annotations = Annotations.Count > 0 ? Annotations : null
        };

        var opts = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(dto, opts);
    }

    public static Edge Deserialize(string json, JsonSerializerOptions? options = null)
    {
        var opts = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var dto = JsonSerializer.Deserialize<EdgeDto>(json, opts)
                  ?? throw new InvalidOperationException("Failed to deserialize Edge JSON.");

        if (dto.Source is null) throw new InvalidOperationException("Edge.source is required.");
        if (dto.SourceHandle is null) throw new InvalidOperationException("Edge.sourceHandle is required.");
        if (dto.Target is null) throw new InvalidOperationException("Edge.target is required.");
        if (dto.TargetHandle is null) throw new InvalidOperationException("Edge.targetHandle is required.");

        var annotations = dto.Annotations ?? new Dictionary<string, object?>();

        // If incoming id is default (empty), generate a new one
        var id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;

        return new Edge(id, dto.Source, dto.SourceHandle, dto.Target, dto.TargetHandle, annotations);
    }

    private class EdgeDto
    {
        public Guid Id { get; set; }
        public string? Source { get; set; }
        public string? SourceHandle { get; set; }
        public string? Target { get; set; }
        public string? TargetHandle { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object?>? Annotations { get; set; }
    }
}

