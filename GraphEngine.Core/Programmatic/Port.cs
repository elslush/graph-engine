using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GraphEngine.Core.Programmatic;

public class Port<T> : INotifyPropertyChanged
{
    public string Name { get; }

    private bool _visible;
    public bool Visible
    {
        get => _visible;
        private set
        {
            if (_visible == value) return;
            _visible = value;
            OnPropertyChanged(nameof(Visible));
        }
    }

    protected object _type; // static type
    protected object? _dynamicType = null; // dynamic type can override static
    protected T _value;

    public object? DynamicType => _dynamicType;

    public object Type => _dynamicType ?? _type;

    public T Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value)) return;
            _value = value;
            OnPropertyChanged(nameof(Value));
        }
    }

    protected readonly List<Edge> _edges = new();
    public ReadOnlyCollection<Edge> Edges => _edges.AsReadOnly();
    public bool IsConnected => _edges.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Port(string name, object type, T value, bool visible = true)
    {
        Name = name;
        _type = type;
        _value = value;
        _visible = visible;
    }

    public void SetVisible(bool visible) => Visible = visible;

    public void SetType(object type)
    {
        // Set the dynamic type (runtime type), mirroring TS setType
        _dynamicType = type;
        OnPropertyChanged(nameof(DynamicType));
        OnPropertyChanged(nameof(Type));
    }

    public void ClearDynamicType()
    {
        if (_dynamicType is null) return;
        _dynamicType = null;
        OnPropertyChanged(nameof(DynamicType));
        OnPropertyChanged(nameof(Type));
    }

    public bool AttachEdge(Edge edge)
    {
        if (_edges.Contains(edge)) return false;
        _edges.Add(edge);
        return true;
    }

    public bool DetachEdge(Edge edge)
    {
        return _edges.Remove(edge);
    }

    public void ClearEdges()
    {
        if (_edges.Count == 0) return;
        _edges.Clear();
    }

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

