using GraphEngine.Core.Programmatic;

namespace GraphEngine.Tests;

public class ProgrammaticInputOutputTests
{
    [Fact]
    public void Input_Reset_ClearsDynamicType_And_SetsDefault()
    {
        var input = new Input<int>("inp", type: new { t = "number" }, value: 123, visible: true);

        // simulate dynamic type by providing a different incoming type
        input.SetValue(456, new Input<int>.SetValueOptions { Type = new { t = "different" } });
        Assert.NotNull(input.DynamicType);

        var v = input.Reset();
        Assert.Null(input.DynamicType);
        Assert.Equal(default(int), v);
        Assert.Equal(default(int), input.Value);
    }

    [Fact]
    public void Input_SetValue_DoesNotStore_WhenConnectedNonVariadic_UnlessForced()
    {
        var input = new Input<string>("in", type: new { t = "string" }, value: "stored");
        var output = new Output<string>("out", type: new { t = "string" }, value: "val");
        output.Connect(input);

        input.SetValue("new", new Input<string>.SetValueOptions { Type = new { t = "string" } });
        Assert.Equal("stored", input.Value); // not stored because connected and not forced

        input.SetValue("forced", new Input<string>.SetValueOptions { ForceStore = true });
        Assert.Equal("forced", input.Value);
    }

    [Fact]
    public void Variadic_Input_Remains_NonArray_WhenAllSame_As_Static()
    {
        var tnum = new { type = "number" };
        var out1 = new Output<int>("o1", tnum, 1);
        var out2 = new Output<int>("o2", tnum, 2);
        var input = new Input<int>("i", type: tnum, value: 0, visible: true, variadic: true);

        out1.Connect(input);
        out2.Connect(input);

        // With static type equal to incoming type, dynamic type stays null per TS logic
        Assert.Null(input.DynamicType);
    }

    [Fact]
    public void Variadic_Input_DynamicType_Null_WhenDifferentTypes()
    {
        var tnum = new { type = "number" };
        var tstr = new { type = "string" };
        var out1 = new Output<int>("o1", tnum, 1);
        var out2 = new Output<string>("o2", tstr, "x");
        var input = new Input<object?>("i", type: tnum, value: null, visible: true, variadic: true);

        out1.Connect(input);
        out2.Connect(input);

        Assert.Null(input.DynamicType);
    }

    [Fact]
    public void Output_Set_Updates_Value_And_DynamicType()
    {
        var output = new Output<int>("o", new { type = "number" }, 1);
        output.Set(5, new { type = "int" });
        Assert.Equal(5, output.Value);
        Assert.NotNull(output.DynamicType);
    }
}
