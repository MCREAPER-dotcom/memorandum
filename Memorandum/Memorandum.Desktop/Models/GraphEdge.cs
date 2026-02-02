namespace Memorandum.Desktop.Models;

public enum GraphEdgeType
{
    InFolder,
    HasTag
}

public sealed class GraphEdge
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public GraphEdgeType Type { get; set; }
}
