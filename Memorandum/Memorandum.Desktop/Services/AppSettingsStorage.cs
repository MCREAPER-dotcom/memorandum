using System;
using System.IO;
using System.Text.Json;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит и загружает дополнительные настройки в LocalApplicationData/Memorandum/settings.json.
/// </summary>
public static class AppSettingsStorage
{
    private const string FileName = "settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetSettingsPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memorandum");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, FileName);
    }

    public static AppSettingsDto Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return new AppSettingsDto();

        try
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<AppSettingsDto>(json, JsonOptions);
            return dto ?? new AppSettingsDto();
        }
        catch
        {
            return new AppSettingsDto();
        }
    }

    public static void Save(AppSettingsDto dto)
    {
        var path = GetSettingsPath();
        var json = JsonSerializer.Serialize(dto ?? new AppSettingsDto(), JsonOptions);
        File.WriteAllText(path, json);
    }
}
