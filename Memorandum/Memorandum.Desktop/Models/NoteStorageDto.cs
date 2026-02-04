using System;
using System.Collections.Generic;

namespace Memorandum.Desktop.Models;

/// <summary>
/// DTO для сериализации заметки в notes.json (без колбэков UI).
/// </summary>
public class NoteStorageDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastEditedAt { get; set; }
    public string Title { get; set; } = "";
    public string Preview { get; set; } = "";
    public string Content { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string FolderName { get; set; } = "";
    public List<string> TagLabels { get; set; } = new();
    public bool IsSticker { get; set; }
    public int? DurationMinutes { get; set; }
    public string BackgroundColorHex { get; set; } = "";
    public int TransparencyPercent { get; set; }
    public bool IsClickThrough { get; set; }
    public bool IsPinned { get; set; }
    public bool CloseOnTimerEnd { get; set; }
    public DateTime? Deadline { get; set; }
}
