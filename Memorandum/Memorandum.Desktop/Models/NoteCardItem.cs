using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Models;

public class NoteCardItem
{
    public string Title { get; }
    /// <summary>Краткий предпросмотр для карточки (отображается в списке).</summary>
    public string Preview { get; }
    /// <summary>Полный текст заметки (редактируется в форме).</summary>
    public string Content { get; }
    public string TypeLabel { get; }
    public string FolderName { get; }
    public IReadOnlyList<string> TagLabels { get; }
    public string DateText { get; }
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
        string dateText,
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
        DateTime? deadline = null)
    {
        Title = title;
        Preview = preview;
        Content = content ?? preview;
        TypeLabel = typeLabel;
        FolderName = folderName;
        TagLabels = tagLabels;
        DateText = dateText;
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

    private static string? NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var s = hex.Trim();
        if (!s.StartsWith("#")) s = "#" + s;
        return s.Length >= 4 ? s : null;
    }
}
