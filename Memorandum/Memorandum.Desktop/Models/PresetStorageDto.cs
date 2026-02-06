using System.Collections.Generic;

namespace Memorandum.Desktop.Models;

/// <summary>
/// DTO для сериализации пресета в presets.json (без UI-зависимостей).
/// </summary>
public class PresetStorageDto
{
    public string Title { get; set; } = "";
    public int TransparencyPercent { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsSticker { get; set; }
    public string ColorHex { get; set; } = "";
    public string? FolderName { get; set; }
    public List<string> TagLabels { get; set; } = new();
}
