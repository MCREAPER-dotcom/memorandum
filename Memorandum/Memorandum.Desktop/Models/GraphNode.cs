namespace Memorandum.Desktop.Models;

public enum GraphNodeType
{
    Note,
    Folder,
    Tag
}

public sealed class GraphNode
{
    public string Id { get; set; } = "";
    public GraphNodeType Type { get; set; }
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public string Color { get; set; } = "#cccccc";

    public double Radius => Type switch
    {
        GraphNodeType.Note => 40,
        GraphNodeType.Folder => 35,
        GraphNodeType.Tag => 30,
        _ => 30
    };

    public static string GetTypeDisplayName(GraphNodeType type) => type switch
    {
        GraphNodeType.Note => "Заметка",
        GraphNodeType.Folder => "Папка",
        GraphNodeType.Tag => "Тег",
        _ => type.ToString()
    };
}
