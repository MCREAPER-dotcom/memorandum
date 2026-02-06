using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит и загружает пресеты в LocalApplicationData/Memorandum/presets.json.
/// </summary>
public static class PresetStorage
{
    private const string FileName = "presets.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetPresetsPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memorandum");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, FileName);
    }

    public static List<PresetStorageDto> Load()
    {
        var path = GetPresetsPath();
        if (!File.Exists(path))
            return GetDefaults();

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<PresetStorageDto>>(json, JsonOptions);
            return list ?? GetDefaults();
        }
        catch
        {
            return GetDefaults();
        }
    }

    public static void Save(IEnumerable<PresetItem> items)
    {
        var path = GetPresetsPath();
        var list = items.Select(ToDto).ToList();
        var json = JsonSerializer.Serialize(list, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static List<PresetStorageDto> GetDefaults()
    {
        return new List<PresetStorageDto>
        {
            new() { Title = "Срочная задача", TransparencyPercent = 95, DurationMinutes = 15, IsSticker = true, ColorHex = "#dc2626" },
            new() { Title = "Напоминание", TransparencyPercent = 90, IsSticker = true, ColorHex = "#fef08a" },
            new() { Title = "Таймер помодоро", TransparencyPercent = 95, DurationMinutes = 25, IsSticker = true, ColorHex = "#2dd4bf" },
            new() { Title = "Обычная заметка", TransparencyPercent = 100, IsSticker = false, ColorHex = "#ffffff" }
        };
    }

    public static PresetItem FromDto(PresetStorageDto dto)
    {
        return new PresetItem(
            dto.Title,
            dto.TransparencyPercent,
            dto.DurationMinutes,
            dto.IsSticker,
            dto.ColorHex ?? "",
            dto.TagLabels,
            dto.FolderName);
    }

    private static PresetStorageDto ToDto(PresetItem item)
    {
        return new PresetStorageDto
        {
            Title = item.Title,
            TransparencyPercent = item.TransparencyPercent,
            DurationMinutes = item.DurationMinutes,
            IsSticker = item.IsSticker,
            ColorHex = item.ColorHex ?? "",
            FolderName = item.FolderName,
            TagLabels = item.TagLabels is List<string> list ? list : new List<string>(item.TagLabels)
        };
    }
}
