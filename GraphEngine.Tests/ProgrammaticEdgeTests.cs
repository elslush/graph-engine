using System;
using System.Collections.Generic;
using System.Text.Json;
using GraphEngine.Core.Programmatic;

namespace GraphEngine.Tests;

public class ProgrammaticEdgeTests
{
    [Fact]
    public void Edge_Serialize_Deserialize_Roundtrip_PreservesData()
    {
        var e1 = new Edge(
            source: "nodeA",
            sourceHandle: "out1",
            target: "nodeB",
            targetHandle: "in1",
            annotations: new Dictionary<string, object?>
            {
                ["num"] = 42,
                ["text"] = "hello"
            });

        var json = e1.Serialize();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("id", out _));
        Assert.Equal("nodeA", root.GetProperty("source").GetString());
        Assert.Equal("out1", root.GetProperty("sourceHandle").GetString());
        Assert.Equal("nodeB", root.GetProperty("target").GetString());
        Assert.Equal("in1", root.GetProperty("targetHandle").GetString());
        Assert.True(root.TryGetProperty("annotations", out var ann));
        Assert.Equal(42, ann.GetProperty("num").GetInt32());
        Assert.Equal("hello", ann.GetProperty("text").GetString());

        var e2 = Edge.Deserialize(json);
        Assert.Equal(e1.Source, e2.Source);
        Assert.Equal(e1.SourceHandle, e2.SourceHandle);
        Assert.Equal(e1.Target, e2.Target);
        Assert.Equal(e1.TargetHandle, e2.TargetHandle);
        Assert.Equal(e1.Annotations.Count, e2.Annotations.Count);
        Assert.NotEqual(Guid.Empty, e2.Id);
    }

    [Fact]
    public void Edge_Serialize_Omits_Empty_Annotations()
    {
        var e = new Edge("A", "out", "B", "in");
        var json = e.Serialize();
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("annotations", out _));
    }

    [Fact]
    public void Edge_Deserialize_GeneratesId_WhenMissing()
    {
        var json = "{" +
                   "\"source\":\"A\"," +
                   "\"sourceHandle\":\"out\"," +
                   "\"target\":\"B\"," +
                   "\"targetHandle\":\"in\"}";

        var e = Edge.Deserialize(json);
        Assert.NotEqual(Guid.Empty, e.Id);
        Assert.Equal("A", e.Source);
        Assert.Equal("out", e.SourceHandle);
        Assert.Equal("B", e.Target);
        Assert.Equal("in", e.TargetHandle);
    }
}

