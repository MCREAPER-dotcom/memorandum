namespace Memorandum.Desktop.Models;

/// <summary>
/// Блок содержимого заметки: текст, ссылка на файл или изображение.
/// </summary>
public abstract class ContentBlockItem
{
    public static ContentBlockItem Text(string text) => new TextContentBlock(text);
    public static ContentBlockItem File(string path, string? displayName = null) => new FileContentBlock(path, displayName);
    public static ContentBlockItem Image(string path) => new ImageContentBlock(path);
}

public sealed class TextContentBlock : ContentBlockItem
{
    public string Text { get; }
    public TextContentBlock(string text) => Text = text ?? "";
}

public sealed class FileContentBlock : ContentBlockItem
{
    public string Path { get; }
    public string DisplayName { get; }
    public FileContentBlock(string path, string? displayName = null)
    {
        Path = (path ?? "").Trim();
        DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : System.IO.Path.GetFileName(Path);
    }

    /// <summary>Строка маркера для удаления из контента: [Файл: path] или [Файл: path|displayName].</summary>
    public string GetMarkerToRemove()
    {
        var nameFromPath = System.IO.Path.GetFileName(Path);
        if (DisplayName != nameFromPath && !string.IsNullOrEmpty(DisplayName))
            return "[Файл: " + Path + "|" + DisplayName + "]";
        return "[Файл: " + Path + "]";
    }
}

public sealed class ImageContentBlock : ContentBlockItem
{
    public string Path { get; }
    public ImageContentBlock(string path) => Path = (path ?? "").Trim();

    public string GetMarkerToRemove() => "[Изображение: " + Path + "]";
}
