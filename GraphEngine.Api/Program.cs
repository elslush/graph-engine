using GraphEngine.Api;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapOpenApi();

app.MapPost("/graph/execute", (ExecuteGraphRequest request) =>
{
    var response = new ExecuteGraphResponse($"Executed: {request.Graph}");
    return Results.Ok(response);
});

app.MapPost("/graph/serialize", (SerializeGraphRequest request) =>
{
    var response = new SerializeGraphResponse($"Serialized: {request.Graph}");
    return Results.Ok(response);
});

app.MapGet("/graph/status", () => Results.Ok("Graph engine is ready"));

app.Run();
