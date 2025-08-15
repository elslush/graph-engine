namespace GraphEngine.Core.Programmatic;

public record struct ConnectionStatus(bool Valid);

public class Output<T> : Port<T>
{
    public Output(string name, object type, T value, bool visible = true)
        : base(name, type, value, visible)
    {
    }

    public void Set(T value, object? type = null)
    {
        Value = value;
        var newDyn = type ?? null;
        if (!Equals(_dynamicType, newDyn))
        {
            _dynamicType = newDyn;
            OnPropertyChanged(nameof(DynamicType));
            OnPropertyChanged(nameof(Type));
        }
    }

    public ConnectionStatus Connect(IInputPort target)
    {
        if (target is null)
        {
            return new ConnectionStatus(false);
        }

        // For non-variadic inputs, replace existing connection
        if (!target.Variadic && target.IsConnected)
        {
            target.ClearEdges();
        }

        var edge = new Edge(
            source: $"output:{Name}",
            sourceHandle: Name,
            target: $"input:{target.Name}",
            targetHandle: target.Name
        );

        // Attach on both ends
        AttachEdge(edge);
        target.RegisterConnectionFrom(Type, edge);

        return new ConnectionStatus(true);
    }
}
