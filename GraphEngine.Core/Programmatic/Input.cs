using System.Text.Json;
using System.Linq;

namespace GraphEngine.Core.Programmatic;

public class Input<T> : Port<T>, IInputPort
{
    public bool Variadic { get; }

    // Track connected source output types for variadic dynamic-type calculation
    private readonly List<object> _connectedSourceTypes = new();

    public Input(string name, object type, T value, bool visible = true, bool variadic = false)
        : base(name, type, value, visible)
    {
        Variadic = variadic;
    }

    public class SetValueOptions
    {
        public bool NoPropagate { get; set; }
        public object? Type { get; set; }
        public bool ForceStore { get; set; }
    }

    public void SetValue(T value, SetValueOptions? opts = null)
    {
        var o = opts ?? new SetValueOptions();

        if (IsConnected && !Variadic && !o.ForceStore)
        {
            if (o.Type is not null)
            {
                HandleTypeUpdate(o.Type);
            }
            // No value update when connected non-variadic (unless forced)
            return;
        }

        // Set stored value and notify
        Value = value;

        if (o.Type is not null)
        {
            HandleTypeUpdate(o.Type);
        }
    }

    private static bool StructurallyEqual(object a, object b)
    {
        // Fallback structural equality via JSON
        var sa = JsonSerializer.Serialize(a);
        var sb = JsonSerializer.Serialize(b);
        return sa == sb;
    }

    private void HandleTypeUpdate(object type)
    {
        // If both have an id property, prefer that comparison path
        var incoming = type;
        var current = _type;

        bool differs = !StructurallyEqual(current, incoming);

        if (!differs)
        {
            return;
        }

        if (Variadic)
        {
            // For variadic inputs, compute dynamic type from all connected source types
            // If all source types are equal, set to { type: 'array', items: <type> }, else null
            // Include the incoming type in the set when provided from the new value
            var types = _connectedSourceTypes.Count > 0 ? _connectedSourceTypes.ToList() : new List<object>();
            if (incoming is not null)
            {
                types.Add(incoming);
            }

            if (types.Count > 0)
            {
                var first = types[0];
                bool allSame = types.All(t => StructurallyEqual(t, first));
                var newDyn = allSame ? new { type = "array", items = first } : null;

                if (!Equals(_dynamicType, newDyn))
                {
                    _dynamicType = newDyn!;
                    OnPropertyChanged(nameof(DynamicType));
                    OnPropertyChanged(nameof(Type));
                }
            }
            else
            {
                if (_dynamicType is not null)
                {
                    _dynamicType = null;
                    OnPropertyChanged(nameof(DynamicType));
                    OnPropertyChanged(nameof(Type));
                }
            }
        }
        else
        {
            // Non-variadic: dynamic type equals incoming type
            _dynamicType = incoming;
            OnPropertyChanged(nameof(DynamicType));
            OnPropertyChanged(nameof(Type));
        }
    }

    public T Reset()
    {
        if (_dynamicType is not null)
        {
            _dynamicType = null;
            OnPropertyChanged(nameof(DynamicType));
            OnPropertyChanged(nameof(Type));
        }

        // No schema-defaults available; use default(T)
        Value = default!;
        return Value;
    }

    public void RegisterConnectionFrom(object sourceType, Edge edge)
    {
        if (AttachEdge(edge))
        {
            _connectedSourceTypes.Add(sourceType);
            if (Variadic)
            {
                // Recompute dynamic type based on all connected types
                HandleTypeUpdate(sourceType);
            }
        }
    }

    internal void UnregisterConnectionFrom(object sourceType, Edge edge)
    {
        if (DetachEdge(edge))
        {
            // remove one occurrence
            var idx = _connectedSourceTypes.FindIndex(t => StructurallyEqual(t, sourceType));
            if (idx >= 0)
            {
                _connectedSourceTypes.RemoveAt(idx);
            }
            if (Variadic)
            {
                // Recompute after removal
                HandleTypeUpdate(sourceType);
            }
        }
    }
}
