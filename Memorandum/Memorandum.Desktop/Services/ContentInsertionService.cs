using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Общий модуль вставки изображений и документов: drag-drop и вставка из буфера.
/// Возвращает строки для вставки в текст заметки ([Изображение: path], [Файл: path], [Папка: path]).
/// </summary>
public static class ContentInsertionService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { "png", "jpg", "jpeg", "gif", "bmp", "webp" };

    /// <summary>
    /// Обрабатывает перетащенные файлы/папки. Возвращает строку для вставки (с переносами) или пустую.
    /// </summary>
    public static string ProcessDroppedPaths(IEnumerable<string> localPaths)
    {
        if (localPaths == null) return "";
        var lines = new List<string>();
        foreach (var path in localPaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            var p = path.Trim();
            if (Directory.Exists(p))
            {
                lines.Add("[Папка: " + p + "]");
                continue;
            }
            if (!File.Exists(p))
                continue;
            var copied = NoteAttachmentsHelper.CopyToAttachments(p);
            var targetPath = copied ?? p;
            var ext = Path.GetExtension(p);
            var originalFileName = Path.GetFileName(p);
            if (ext.Length > 1 && ImageExtensions.Contains(ext.AsSpan(1).ToString()))
                lines.Add("[Изображение: " + targetPath + "]");
            else
                lines.Add("[Файл: " + targetPath + "|" + originalFileName + "]");
        }
        if (lines.Count == 0) return "";
        return Environment.NewLine + string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    /// <summary>
    /// Получает из буфера текст или изображение. getTextAsync — получение текста из буфера (например через IClipboard.GetTextAsync); изображение сохраняет во вложения и возвращает [Изображение: path]. Иначе null.
    /// </summary>
    public static async Task<string?> GetPastedContentAsync(Func<Task<string?>>? getTextAsync, IntPtr windowHandle)
    {
        if (getTextAsync != null)
        {
            var text = await getTextAsync();
            if (!string.IsNullOrEmpty(text))
                return text;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && windowHandle != IntPtr.Zero)
        {
            var imagePath = ScreenshotClipboardService.GetImageFromClipboardAndSaveToFile(windowHandle);
            if (!string.IsNullOrEmpty(imagePath))
                return Environment.NewLine + "[Изображение: " + imagePath + "]" + Environment.NewLine;
        }

        return null;
    }
}
