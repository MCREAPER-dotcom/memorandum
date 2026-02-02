namespace Memorandum.Desktop.Models;

/// <summary>
/// Блок содержимого заметки: текст, ссылка на файл или изображение.
/// </summary>
public abstract class ContentBlockItem
{
    public static ContentBlockItem Text(string text) => new TextContentBlock(text);
    public static ContentBlockItem File(string path) => new FileContentBlock(path);
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
    public string DisplayName => System.IO.Path.GetFileName(Path.Trim());
    public FileContentBlock(string path) => Path = (path ?? "").Trim();
}

public sealed class ImageContentBlock : ContentBlockItem
{
    public string Path { get; }
    public ImageContentBlock(string path) => Path = (path ?? "").Trim();
}
