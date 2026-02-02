using System.Collections.Generic;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

public sealed class MockGraphDataProvider : IGraphDataProvider
{
    private static readonly List<GraphNode> Nodes = new()
    {
        new GraphNode { Id = "f1", Type = GraphNodeType.Folder, Label = "Работа", X = 200, Y = 200, Color = "#4ecdc4" },
        new GraphNode { Id = "f2", Type = GraphNodeType.Folder, Label = "Личное", X = 500, Y = 200, Color = "#95e1d3" },
        new GraphNode { Id = "t1", Type = GraphNodeType.Tag, Label = "важное", X = 100, Y = 400, Color = "#ff6b6b" },
        new GraphNode { Id = "t2", Type = GraphNodeType.Tag, Label = "встреча", X = 250, Y = 450, Color = "#4ecdc4" },
        new GraphNode { Id = "t3", Type = GraphNodeType.Tag, Label = "покупки", X = 450, Y = 400, Color = "#95e1d3" },
        new GraphNode { Id = "t4", Type = GraphNodeType.Tag, Label = "идеи", X = 600, Y = 450, Color = "#ffd93d" },
        new GraphNode { Id = "n1", Type = GraphNodeType.Note, Label = "Встреча с командой", X = 200, Y = 350, Color = "#e8f5e9" },
        new GraphNode { Id = "n2", Type = GraphNodeType.Note, Label = "Купить продукты", X = 500, Y = 350, Color = "#fff9c4" },
        new GraphNode { Id = "n3", Type = GraphNodeType.Note, Label = "Идеи для проекта", X = 350, Y = 300, Color = "#e3f2fd" }
    };

    private static readonly List<GraphEdge> Edges = new()
    {
        new GraphEdge { From = "n1", To = "f1", Type = GraphEdgeType.InFolder },
        new GraphEdge { From = "n2", To = "f2", Type = GraphEdgeType.InFolder },
        new GraphEdge { From = "n3", To = "f1", Type = GraphEdgeType.InFolder },
        new GraphEdge { From = "n1", To = "t1", Type = GraphEdgeType.HasTag },
        new GraphEdge { From = "n1", To = "t2", Type = GraphEdgeType.HasTag },
        new GraphEdge { From = "n2", To = "t3", Type = GraphEdgeType.HasTag },
        new GraphEdge { From = "n3", To = "t4", Type = GraphEdgeType.HasTag }
    };

    public IReadOnlyList<GraphNode> GetNodes() => Nodes;
    public IReadOnlyList<GraphEdge> GetEdges() => Edges;
}
