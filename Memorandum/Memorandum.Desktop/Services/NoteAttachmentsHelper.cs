using System.IO;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит пути к папке вложений заметок и копирует файлы в неё.
/// </summary>
public static class NoteAttachmentsHelper
{
    private static string? _attachmentsFolder;

    public static string GetAttachmentsFolder()
    {
        if (_attachmentsFolder != null)
            return _attachmentsFolder;
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memorandum",
            "Data",
            "Attachments");
        Directory.CreateDirectory(baseDir);
        _attachmentsFolder = baseDir;
        return baseDir;
    }

    /// <summary>
    /// Копирует файл в папку вложений с уникальным именем. Возвращает полный путь к копии или null при ошибке.
    /// </summary>
    public static string? CopyToAttachments(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            return null;
        var dir = GetAttachmentsFolder();
        var fileName = Path.GetFileName(sourceFilePath);
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
            ext = ".bin";
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var destPath = Path.Combine(dir, uniqueName);
        try
        {
            File.Copy(sourceFilePath, destPath, overwrite: false);
            return destPath;
        }
        catch
        {
            return null;
        }
    }
}
