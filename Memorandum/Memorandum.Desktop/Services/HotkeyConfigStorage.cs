using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит и загружает настройки горячих клавиш отдельно от логики приложения.
/// Файл: LocalApplicationData/Memorandum/hotkeys.json
/// </summary>
public static class HotkeyConfigStorage
{
    private const string FileName = "hotkeys.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetConfigPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memorandum");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, FileName);
    }

    public static IReadOnlyList<HotkeyConfigItem> Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
            return GetDefaults();

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<HotkeyConfigItem>>(json);
            if (list == null || list.Count == 0)
                return GetDefaults();
            var defaults = GetDefaults();
            var result = new List<HotkeyConfigItem>();
            foreach (var d in defaults)
            {
                var saved = list.Find(x => string.Equals(x.ActionId, d.ActionId, StringComparison.OrdinalIgnoreCase));
                result.Add(saved != null
                    ? new HotkeyConfigItem { ActionId = d.ActionId, DisplayName = d.DisplayName, KeyCombo = saved.KeyCombo ?? "" }
                    : d);
            }
            return result;
        }
        catch
        {
            return GetDefaults();
        }
    }

    public static void Save(IEnumerable<HotkeyConfigItem> items)
    {
        var path = GetConfigPath();
        var list = items.ToList();
        var json = JsonSerializer.Serialize(list, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static IReadOnlyList<HotkeyConfigItem> GetDefaults()
    {
        return new List<HotkeyConfigItem>
        {
            new() { ActionId = "LaunchApp", DisplayName = "Запуск приложения", KeyCombo = "Ctrl+Alt+M" },
            new() { ActionId = "LaunchNote", DisplayName = "Запуск заметки", KeyCombo = "Ctrl+Alt+N" },
            new() { ActionId = "HideShowNote", DisplayName = "Скрыть/показать заметку", KeyCombo = "Ctrl+Alt+H" },
            new() { ActionId = "LaunchPreset", DisplayName = "Запуск заметки из пресета", KeyCombo = "" }
        };
    }
}
