using System.Collections.Generic;

namespace Memorandum.Desktop.Models;

/// <summary>
/// DTO для сериализации состояния приложения (папки, цвета тегов) в appstate.json.
/// </summary>
public class AppStateDto
{
    /// <summary>Дополнительные пути папок, добавленные пользователем (без дублирования с папками из заметок).</summary>
    public List<string> FolderPaths { get; set; } = new();

    /// <summary>Соответствие имени тега ключу цвета в палитре (TagName -> ColorKey).</summary>
    public Dictionary<string, string> TagColorKeys { get; set; } = new();
}
