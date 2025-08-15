using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphEngine.Core.Programmatic;

public class Node
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Arbitrary metadata on the node
    public Dictionary<string, object?> Annotations { get; set; } = new();

    // Ports
    public Dictionary<string, object> Inputs { get; } = new(); // values are Input<T>
    public Dictionary<string, object> Outputs { get; } = new(); // values are Output<T>

    public double LastExecutedDurationMs { get; private set; }
    public Exception? Error { get; private set; }
    public bool IsRunning { get; private set; }

    // Hooks
    public virtual void OnStart() { }
    public virtual void OnStop() { }
    public virtual void OnPause() => OnStop();
    public virtual void OnResume() => OnStart();

    public Node() { }

    public Node(Guid id)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
    }

    public Input<T> AddInput<T>(string name, TypeDefinition type)
    {
        var input = new Input<T>(
            name: name,
            type: type.Type,
            value: default!,
            visible: type.Visible ?? true,
            variadic: type.Variadic ?? false
        );
        Inputs[name] = input;
        return input;
    }

    public Output<T> AddOutput<T>(string name, TypeDefinition type)
    {
        var output = new Output<T>(
            name: name,
            type: type.Type,
            value: default!,
            visible: type.Visible ?? true
        );
        Outputs[name] = output;
        return output;
    }

    public virtual Task Execute() => Task.CompletedTask;

    public async Task<NodeRun> Run()
    {
        IsRunning = true;
        var sw = Stopwatch.StartNew();
        var start = DateTimeOffset.UtcNow;
        try
        {
            await Execute().ConfigureAwait(false);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = ex;
        }
        finally
        {
            sw.Stop();
            IsRunning = false;
            LastExecutedDurationMs = sw.Elapsed.TotalMilliseconds;
        }

        var end = DateTimeOffset.UtcNow;
        return new NodeRun(this, Error, start, end);
    }

    public Node Clone()
    {
        var clone = (Node)Activator.CreateInstance(GetType())!;
        clone.Annotations = new Dictionary<string, object?>(Annotations);

        // New ID for the clone
        // Note: If you want to preserve ID, change here
        // We follow TS behavior to assign a fresh unique ID
        clone.Id = Guid.NewGuid();

        // Clone inputs
        foreach (var (key, value) in Inputs)
        {
            var t = value.GetType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Input<>))
            {
                var genArg = t.GetGenericArguments()[0];
                var boxedValue = (object?)t.GetProperty("BoxedValue")!.GetValue(value);
                var typeObj = t.BaseType!.GetField("_type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(value)!;
                var visible = (bool)t.BaseType!.GetProperty("Visible")!.GetValue(value)!;
                var variadic = (bool)t.GetProperty("Variadic")!.GetValue(value)!;

                var inputType = typeof(Input<>).MakeGenericType(genArg);
                var ctor = inputType.GetConstructor(new[] { typeof(string), typeof(object), genArg, typeof(bool), typeof(bool) });
                var newInput = ctor!.Invoke(new object?[] { key, typeObj, boxedValue, visible, variadic });
                clone.Inputs[key] = newInput!;
            }
        }

        // Clone outputs
        foreach (var (key, value) in Outputs)
        {
            var t = value.GetType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Output<>))
            {
                var genArg = t.GetGenericArguments()[0];
                var boxedValue = (object?)t.GetProperty("BoxedValue")!.GetValue(value);
                var typeObj = t.BaseType!.GetField("_type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(value)!;
                var visible = (bool)t.BaseType!.GetProperty("Visible")!.GetValue(value)!;

                var outType = typeof(Output<>).MakeGenericType(genArg);
                var ctor = outType.GetConstructor(new[] { typeof(string), typeof(object), genArg, typeof(bool) });
                var newOutput = ctor!.Invoke(new object?[] { key, typeObj, boxedValue, visible });
                clone.Outputs[key] = newOutput!;
            }
        }

        return clone;
    }

    public string Serialize(JsonSerializerOptions? options = null)
    {
        var dto = new SerializedNode
        {
            Id = Id,
            Type = GetType().Name,
            Inputs = Inputs.Select(kvp => SerializeInput(kvp.Key, kvp.Value)).ToList()
        };
        if (Annotations.Count > 0)
        {
            dto.Annotations = Annotations;
        }

        var opts = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(dto, opts);
    }

    public static Node Deserialize(string json, Func<Node>? factory = null, JsonSerializerOptions? options = null)
    {
        var node = factory?.Invoke() ?? new Node();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out var gid)
            ? gid
            : Guid.Empty;
        node.Id = id == Guid.Empty ? Guid.NewGuid() : id;

        if (root.TryGetProperty("annotations", out var annEl) && annEl.ValueKind == JsonValueKind.Object)
        {
            node.Annotations = JsonSerializer.Deserialize<Dictionary<string, object?>>(annEl.GetRawText())
                               ?? new Dictionary<string, object?>();
        }

        if (root.TryGetProperty("inputs", out var inputsEl) && inputsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in inputsEl.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString() ?? throw new InvalidOperationException("Input missing name.");
                var typeObj = item.TryGetProperty("type", out var typeEl)
                    ? JsonSerializer.Deserialize<object>(typeEl.GetRawText()) ?? new object()
                    : new object();
                var visible = item.TryGetProperty("visible", out var visEl) && visEl.ValueKind == JsonValueKind.False ? false : true;
                var variadic = item.TryGetProperty("variadic", out var varEl) && varEl.ValueKind == JsonValueKind.True;

                var input = new Input<object?>(name, typeObj, default!, visible, variadic);

                if (item.TryGetProperty("dynamicType", out var dynEl))
                {
                    var dyn = JsonSerializer.Deserialize<object>(dynEl.GetRawText());
                    if (dyn is not null) input.SetType(dyn);
                }
                if (item.TryGetProperty("value", out var valEl))
                {
                    var val = JsonSerializer.Deserialize<object?>(valEl.GetRawText());
                    input.SetValue(val, new Input<object?>.SetValueOptions { ForceStore = true });
                }
                node.Inputs[name] = input;
            }
        }

        return node;
    }

    private static SerializedInput SerializeInput(string name, object inputObj)
    {
        var t = inputObj.GetType();
        var isInput = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Input<>);
        if (!isInput)
        {
            throw new InvalidOperationException($"Port '{name}' is not an Input<>, cannot serialize.");
        }

        var baseType = t.BaseType!; // Port<>
        var typeObj = baseType.GetField("_type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(inputObj);
        var visible = (bool)baseType.GetProperty("Visible")!.GetValue(inputObj)!;
        var dynamicType = baseType.GetProperty("DynamicType")!.GetValue(inputObj);
        var isConnected = (bool)baseType.GetProperty("IsConnected")!.GetValue(inputObj)!;
        var boxedValue = t.GetProperty("BoxedValue")!.GetValue(inputObj);
        var variadic = (bool)t.GetProperty("Variadic")!.GetValue(inputObj)!;

        var si = new SerializedInput
        {
            Name = name,
            Type = typeObj,
            Variadic = variadic,
            Visible = visible,
            DynamicType = dynamicType,
        };

        // Only include value if not a connected non-variadic input
        if (!isConnected || variadic)
        {
            si.Value = boxedValue;
            si.HasValue = true;
        }

        return si;
    }
}

public record struct NodeRun(Node Node, Exception? Error, DateTimeOffset Start, DateTimeOffset End);

public class TypeDefinition
{
    public required object Type { get; init; }
    public bool? Variadic { get; init; }
    public bool? Visible { get; init; }
}

internal sealed class SerializedNode
{
    public Guid Id { get; set; }
    public string? Type { get; set; }
    public List<SerializedInput>? Inputs { get; set; }
    public Dictionary<string, object?>? Annotations { get; set; }
}

internal sealed class SerializedInput
{
    public required string Name { get; set; }
    public object? Type { get; set; }
    public object? DynamicType { get; set; }
    public bool? Variadic { get; set; }
    public bool? Visible { get; set; }
    public object? Value { get; set; }

    [JsonIgnore]
    public bool HasValue { get; set; }
}
