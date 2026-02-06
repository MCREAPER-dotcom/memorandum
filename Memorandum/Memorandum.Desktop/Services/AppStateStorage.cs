using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит и загружает состояние приложения (папки, цвета тегов) в LocalApplicationData/Memorandum/appstate.json.
/// </summary>
public static class AppStateStorage
{
    private const string FileName = "appstate.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetAppStatePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memorandum");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, FileName);
    }

    public static AppStateDto Load()
    {
        var path = GetAppStatePath();
        if (!File.Exists(path))
            return new AppStateDto();

        try
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<AppStateDto>(json, JsonOptions);
            return dto ?? new AppStateDto();
        }
        catch
        {
            return new AppStateDto();
        }
    }

    public static void Save(IReadOnlyList<string> folderPaths, IReadOnlyDictionary<string, string> tagColorKeys)
    {
        var path = GetAppStatePath();
        var dto = new AppStateDto
        {
            FolderPaths = folderPaths?.ToList() ?? new List<string>(),
            TagColorKeys = tagColorKeys != null ? new Dictionary<string, string>(tagColorKeys) : new Dictionary<string, string>()
        };
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(path, json);
    }
}
