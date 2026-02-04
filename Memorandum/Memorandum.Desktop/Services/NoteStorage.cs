using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Хранит и загружает заметки в LocalApplicationData/Memorandum/notes.json.
/// </summary>
public static class NoteStorage
{
    private const string FileName = "notes.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetNotesPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memorandum");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, FileName);
    }

    public static List<NoteStorageDto> Load()
    {
        var path = GetNotesPath();
        if (!File.Exists(path))
            return new List<NoteStorageDto>();

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<NoteStorageDto>>(json, JsonOptions);
            return list ?? new List<NoteStorageDto>();
        }
        catch
        {
            return new List<NoteStorageDto>();
        }
    }

    public static void Save(IEnumerable<NoteStorageDto> items)
    {
        var path = GetNotesPath();
        var list = new List<NoteStorageDto>(items);
        var json = JsonSerializer.Serialize(list, JsonOptions);
        File.WriteAllText(path, json);
    }
}
