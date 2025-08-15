using System.Text.Json;
using System.Linq;
using GraphEngine.Core.Programmatic;

namespace GraphEngine.Tests;

public class ProgrammaticNodeTests
{
    private sealed class ThrowingNode : Node
    {
        public override Task Execute()
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class SuccessNode : Node
    {
        public override Task Execute()
        {
            // do nothing
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void AddInput_AddOutput_CreatesPorts()
    {
        var n = new Node();
        var tnum = new { type = "number" };
        var input = n.AddInput<int>("a", new TypeDefinition { Type = tnum, Variadic = true, Visible = false });
        var output = n.AddOutput<int>("b", new TypeDefinition { Type = tnum, Visible = true });

        Assert.True(n.Inputs.ContainsKey("a"));
        Assert.True(n.Outputs.ContainsKey("b"));
        Assert.False(input.Visible);
        Assert.True(input.Variadic);
        Assert.Equal(tnum, input.Type);
        Assert.Equal(tnum, output.Type);
    }

    [Fact]
    public void Serialize_Includes_Value_For_NonConnected_Input()
    {
        var n = new Node();
        var tnum = new { type = "number" };
        var inp = n.AddInput<int>("x", new TypeDefinition { Type = tnum, Visible = true });
        var connected = n.AddInput<int>("y", new TypeDefinition { Type = tnum, Visible = true });
        // Set values
        inp.SetValue(42);
        connected.AttachEdge(new Edge("src","out","dst","in"));

        var json = n.Serialize();
        using var doc = JsonDocument.Parse(json);
        var inputs = doc.RootElement.GetProperty("inputs").EnumerateArray().ToArray();
        var x = inputs.First(i => i.GetProperty("name").GetString() == "x");
        var y = inputs.First(i => i.GetProperty("name").GetString() == "y");
        Assert.True(x.TryGetProperty("value", out _));
        Assert.False(y.TryGetProperty("value", out _));
    }

    [Fact]
    public void Deserialize_Restores_Inputs_And_Values()
    {
        var n = new Node();
        var ts = new { type = "string" };
        var inp = n.AddInput<string>("s", new TypeDefinition { Type = ts, Visible = true });
        inp.SetValue("hello");
        var json = n.Serialize();

        var n2 = Node.Deserialize(json);
        Assert.True(n2.Inputs.ContainsKey("s"));
        var sInput = (Input<object?>)n2.Inputs["s"];
        var val = sInput.Value;
        if (val is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            Assert.Equal("hello", je.GetString());
        }
        else
        {
            Assert.Equal("hello", val);
        }
    }

    [Fact]
    public async Task Run_Captures_Error_And_Duration()
    {
        var n1 = new ThrowingNode();
        var result1 = await n1.Run();
        Assert.NotNull(result1.Error);
        Assert.True(n1.LastExecutedDurationMs >= 0);
        Assert.False(n1.IsRunning);

        var n2 = new SuccessNode();
        var result2 = await n2.Run();
        Assert.Null(result2.Error);
        Assert.False(n2.IsRunning);
    }
}
