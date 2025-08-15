namespace GraphEngine.Api;

public record ExecuteGraphRequest(string Graph);
public record ExecuteGraphResponse(string Result);

public record SerializeGraphRequest(string Graph);
public record SerializeGraphResponse(string Serialized);
