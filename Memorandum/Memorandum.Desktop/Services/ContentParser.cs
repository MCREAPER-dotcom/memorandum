using System.Collections.Generic;
using System.Text.RegularExpressions;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Парсит сырой текст заметки в блоки: текст, [Файл: path], [Изображение: path], [Папка: path].
/// </summary>
public static class ContentParser
{
    private static readonly Regex FileRegex = new Regex(
        @"\[Файл:\s*([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ImageRegex = new Regex(
        @"\[Изображение:\s*([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex FolderRegex = new Regex(
        @"\[Папка:\s*([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<ContentBlockItem> Parse(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return new List<ContentBlockItem>();

        var blocks = new List<ContentBlockItem>();
        var combined = new Regex(
            @"(\[Файл:\s*[^\]]*\]|\[Изображение:\s*[^\]]*\]|\[Папка:\s*[^\]]*\])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var lastIndex = 0;
        foreach (Match m in combined.Matches(content))
        {
            if (m.Index > lastIndex)
            {
                var text = content.Substring(lastIndex, m.Index - lastIndex).Trim();
                if (text.Length > 0)
                    blocks.Add(ContentBlockItem.Text(text));
            }

            var token = m.Value;
            if (FileRegex.Match(token) is { Success: true } fileMatch)
            {
                var fileValue = fileMatch.Groups[1].Value.Trim();
                var pipe = fileValue.IndexOf('|');
                if (pipe >= 0)
                {
                    var path = fileValue.Substring(0, pipe).Trim();
                    var displayName = fileValue.Substring(pipe + 1).Trim();
                    blocks.Add(ContentBlockItem.File(path, displayName));
                }
                else
                    blocks.Add(ContentBlockItem.File(fileValue));
            }
            else if (ImageRegex.Match(token) is { Success: true } imgMatch)
                blocks.Add(ContentBlockItem.Image(imgMatch.Groups[1].Value.Trim()));
            else if (FolderRegex.Match(token) is { Success: true } folderMatch)
            {
                var folderValue = folderMatch.Groups[1].Value.Trim();
                var folderPipe = folderValue.IndexOf('|');
                if (folderPipe >= 0)
                {
                    var path = folderValue.Substring(0, folderPipe).Trim();
                    var displayName = folderValue.Substring(folderPipe + 1).Trim();
                    blocks.Add(ContentBlockItem.File(path, displayName));
                }
                else
                    blocks.Add(ContentBlockItem.File(folderValue));
            }

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < content.Length)
        {
            var text = content.Substring(lastIndex).Trim();
            if (text.Length > 0)
            {
                var imageLoose = LooseImageRegex.Match(text);
                if (imageLoose.Success && imageLoose.Index == 0)
                {
                    var path = imageLoose.Groups[1].Value.Trim();
                    if (path.Length > 0)
                        blocks.Add(ContentBlockItem.Image(path));
                    var rest = text.Substring(imageLoose.Length).Trim();
                    if (rest.Length > 0)
                        blocks.Add(ContentBlockItem.Text(rest));
                }
                else
                    blocks.Add(ContentBlockItem.Text(text));
            }
        }

        return blocks;
    }

    private static readonly Regex LooseImageRegex = new Regex(
        @"\[Изображение:\s*([^\]\r\n]*)\]?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
