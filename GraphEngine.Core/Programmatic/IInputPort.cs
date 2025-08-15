namespace GraphEngine.Core.Programmatic;

public interface IInputPort
{
    string Name { get; }
    bool Variadic { get; }
    bool IsConnected { get; }
    bool AttachEdge(Edge edge);
    void ClearEdges();
    void RegisterConnectionFrom(object sourceType, Edge edge);
}

