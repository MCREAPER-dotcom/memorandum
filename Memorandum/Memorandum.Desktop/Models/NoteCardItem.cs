using System;
using System.Collections.Generic;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Models;

public class NoteCardItem
{
    public Guid Id { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastEditedAt { get; }

    public string Title { get; }
    /// <summary>Краткий предпросмотр для карточки (отображается в списке).</summary>
    public string Preview { get; }
    /// <summary>Полный текст заметки (редактируется в форме).</summary>
    public string Content { get; }
    public string TypeLabel { get; }
    public string FolderName { get; }
    public IReadOnlyList<string> TagLabels { get; }
    public string DateText => LastEditedAt.ToString("dd.MM.yyyy");
    public bool IsSticker { get; }
    public bool IsNormal => !IsSticker;
    public int? DurationMinutes { get; }
    public bool HasTimer => DurationMinutes.HasValue;
    public string TimerDisplayText => DurationMinutes.HasValue ? $"Таймер: {DurationMinutes} мин" : "";
    public string BackgroundColorHex { get; }
    public int TransparencyPercent { get; }
    public bool IsClickThrough { get; }
    public bool IsPinned { get; }
    public bool CloseOnTimerEnd { get; }
    public DateTime? Deadline { get; }
    public bool HasDeadline => Deadline.HasValue;
    public string DeadlineDisplayText => Deadline.HasValue ? Deadline.Value.ToString("dd.MM.yyyy HH:mm") : "";

    private readonly Action _onEdit;
    private readonly Action _onOpenSticker;
    private readonly Action _onStartTimer;
    private readonly Action _onPauseTimer;

    public NoteCardItem(
        string title,
        string preview,
        string content,
        string typeLabel,
        string folderName,
        IReadOnlyList<string> tagLabels,
        bool isSticker,
        Action onEdit,
        Action onOpenSticker,
        int? durationMinutes = null,
        Action? onStartTimer = null,
        Action? onPauseTimer = null,
        string? backgroundColorHex = null,
        int transparencyPercent = 100,
        bool isClickThrough = false,
        bool isPinned = true,
        bool closeOnTimerEnd = false,
        DateTime? deadline = null,
        Guid? id = null,
        DateTime? createdAt = null,
        DateTime? lastEditedAt = null)
    {
        Id = id ?? Guid.NewGuid();
        var now = DateTime.UtcNow;
        CreatedAt = createdAt ?? now;
        LastEditedAt = lastEditedAt ?? now;
        Title = title;
        Preview = preview;
        Content = content ?? preview;
        TypeLabel = typeLabel;
        FolderName = folderName;
        TagLabels = tagLabels ?? Array.Empty<string>();
        IsSticker = isSticker;
        DurationMinutes = durationMinutes;
        BackgroundColorHex = NormalizeHex(backgroundColorHex) ?? PaletteConstants.DefaultStickerBackgroundHex;
        TransparencyPercent = Math.Clamp(transparencyPercent, 1, 100);
        IsClickThrough = isClickThrough;
        IsPinned = isPinned;
        CloseOnTimerEnd = closeOnTimerEnd;
        Deadline = deadline;
        _onEdit = onEdit;
        _onOpenSticker = onOpenSticker;
        _onStartTimer = onStartTimer ?? (() => { });
        _onPauseTimer = onPauseTimer ?? (() => { });
    }

    public void Edit(object _) => _onEdit();
    public void OpenSticker(object _) => _onOpenSticker();
    public void StartTimer(object _) => _onStartTimer();
    public void PauseTimer(object _) => _onPauseTimer();

    public NoteStorageDto ToStorageDto()
    {
        return new NoteStorageDto
        {
            Id = Id,
            CreatedAt = CreatedAt,
            LastEditedAt = LastEditedAt,
            Title = Title,
            Preview = Preview,
            Content = Content,
            TypeLabel = TypeLabel,
            FolderName = FolderName,
            TagLabels = TagLabels is List<string> list ? list : new List<string>(TagLabels),
            IsSticker = IsSticker,
            DurationMinutes = DurationMinutes,
            BackgroundColorHex = BackgroundColorHex,
            TransparencyPercent = TransparencyPercent,
            IsClickThrough = IsClickThrough,
            IsPinned = IsPinned,
            CloseOnTimerEnd = CloseOnTimerEnd,
            Deadline = Deadline
        };
    }

    /// <summary>
    /// Создаёт NoteCardItem из DTO; колбэки задаются снаружи (при загрузке списка).
    /// </summary>
    public static NoteCardItem FromStorageDto(
        NoteStorageDto dto,
        Action onEdit,
        Action onOpenSticker,
        Action? onStartTimer = null,
        Action? onPauseTimer = null)
    {
        return new NoteCardItem(
            dto.Title,
            dto.Preview,
            dto.Content,
            dto.TypeLabel,
            dto.FolderName ?? "",
            dto.TagLabels ?? new List<string>(),
            dto.IsSticker,
            onEdit,
            onOpenSticker,
            dto.DurationMinutes,
            onStartTimer,
            onPauseTimer,
            dto.BackgroundColorHex,
            dto.TransparencyPercent,
            dto.IsClickThrough,
            dto.IsPinned,
            dto.CloseOnTimerEnd,
            dto.Deadline,
            dto.Id,
            dto.CreatedAt,
            dto.LastEditedAt);
    }

    private static string? NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var s = hex.Trim();
        if (!s.StartsWith("#")) s = "#" + s;
        return s.Length >= 4 ? s : null;
    }
}
