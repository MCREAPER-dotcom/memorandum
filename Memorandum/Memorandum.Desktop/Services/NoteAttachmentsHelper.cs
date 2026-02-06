using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит пути к папке вложений заметок и копирует файлы в неё.
/// </summary>
public static class NoteAttachmentsHelper
{
    private static string? _attachmentsFolder;

    /// <summary>Префикс относительного пути вложений в сохранённом контенте.</summary>
    public const string AttachmentsPathPrefix = "Attachments:";

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

    /// <summary>Для сохранения: заменяет полные пути вложений на Attachments:fileName.</summary>
    public static string NormalizeContentForStorage(string? content)
    {
        if (string.IsNullOrEmpty(content)) return content ?? "";
        var dir = GetAttachmentsFolder().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var escapedDir = Regex.Escape(dir);
        var pattern = @"(\[(?:Файл|Изображение|Папка):\s*)(" + escapedDir + @"[\\/])([^\]]+)\]";
        return Regex.Replace(content, pattern, m =>
        {
            var rest = m.Groups[3].Value;
            var pipe = rest.IndexOf('|');
            var pathPart = pipe >= 0 ? rest.Substring(0, pipe).Trim() : rest;
            var suffix = pipe >= 0 ? "|" + rest.Substring(pipe + 1) : "";
            return m.Groups[1].Value + AttachmentsPathPrefix + pathPart + suffix + "]";
        }, RegexOptions.IgnoreCase);
    }

    /// <summary>После загрузки: восстанавливает полные пути для Attachments:fileName.</summary>
    public static string ResolveContentPaths(string? content)
    {
        if (string.IsNullOrEmpty(content)) return content ?? "";
        var dir = GetAttachmentsFolder();
        var pattern = @"(\[(?:Файл|Изображение|Папка):\s*)" + Regex.Escape(AttachmentsPathPrefix) + @"([^\]]+)\]";
        return Regex.Replace(content, pattern, m =>
        {
            var rest = m.Groups[2].Value.Trim();
            var pipe = rest.IndexOf('|');
            var pathPart = pipe >= 0 ? rest.Substring(0, pipe).Trim() : rest;
            var suffix = pipe >= 0 ? "|" + rest.Substring(pipe + 1) : "";
            return m.Groups[1].Value + Path.Combine(dir, pathPart).TrimEnd() + suffix + "]";
        }, RegexOptions.IgnoreCase);
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
