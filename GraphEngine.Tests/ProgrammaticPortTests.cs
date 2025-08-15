using System.Collections.Generic;
using GraphEngine.Core.Programmatic;

namespace GraphEngine.Tests;

public class ProgrammaticPortTests
{
    [Fact]
    public void SetVisible_RaisesPropertyChanged()
    {
        var port = new Port<int>(name: "p", type: new object(), value: 0, visible: true);
        var changed = new List<string>();
        port.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        port.SetVisible(false);

        Assert.Contains("Visible", changed);
        Assert.False(port.Visible);
    }

    [Fact]
    public void SetType_RaisesPropertyChanged_ForTypeAndDynamicType()
    {
        var port = new Port<string>(name: "p", type: "static", value: "");
        var changed = new List<string>();
        port.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        var dyn = new { schema = "dynamic" };
        port.SetType(dyn);

        Assert.Equal(dyn, port.DynamicType);
        Assert.Equal(dyn, port.Type);
        Assert.Contains("DynamicType", changed);
        Assert.Contains("Type", changed);

        changed.Clear();
        port.ClearDynamicType();
        Assert.Null(port.DynamicType);
        Assert.Equal("static", port.Type);
        Assert.Contains("DynamicType", changed);
        Assert.Contains("Type", changed);
    }

    [Fact]
    public void SettingValue_RaisesPropertyChanged()
    {
        var port = new Port<int>(name: "p", type: new object(), value: 1);
        var changed = new List<string>();
        port.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        port.Value = 2;

        Assert.Contains("Value", changed);
        Assert.Equal(2, port.Value);
    }

    [Fact]
    public void SetVisible_SameValue_DoesNotRaise()
    {
        var port = new Port<int>(name: "p", type: new object(), value: 0, visible: false);
        var changed = new List<string>();
        port.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        port.SetVisible(false);

        Assert.DoesNotContain("Visible", changed);
    }

    [Fact]
    public void SettingValue_Same_DoesNotRaise()
    {
        var port = new Port<string>(name: "p", type: new object(), value: "x");
        var changed = new List<string>();
        port.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        port.Value = "x";

        Assert.DoesNotContain("Value", changed);
    }

    [Fact]
    public void EdgeTracking_AttachDetach_Works()
    {
        var port = new Port<string>(name: "p", type: new object(), value: "v");
        var edge = new Edge("nodeA", "out", "nodeB", "in");

        Assert.False(port.IsConnected);
        Assert.Empty(port.Edges);

        var added = port.AttachEdge(edge);
        Assert.True(added);
        Assert.True(port.IsConnected);
        Assert.Single(port.Edges);

        var addedAgain = port.AttachEdge(edge);
        Assert.False(addedAgain);
        Assert.Single(port.Edges);

        var removed = port.DetachEdge(edge);
        Assert.True(removed);
        Assert.False(port.IsConnected);
        Assert.Empty(port.Edges);
    }
}
