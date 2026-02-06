using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Memorandum.Desktop.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.ViewModels;

public sealed partial class NoteCardViewModel : ViewModelBase
{
    private readonly NoteCardItem _note;
    private readonly IReadOnlyList<ContentBlockItem> _contentBlocks;

    [ObservableProperty]
    private bool _isHighlighted;

    public NoteCardViewModel(NoteCardItem note)
    {
        _note = note;
        _contentBlocks = ContentParser.Parse(_note.Content);
        EditCommand = new RelayCommand(() => _note.Edit(null!));
        OpenStickerCommand = new RelayCommand(() => _note.OpenSticker(null!));
        StartTimerCommand = new RelayCommand(() => _note.StartTimer(null!));
        PauseTimerCommand = new RelayCommand(() => _note.PauseTimer(null!));
    }

    public NoteCardItem Note => _note;

    public string Title => _note.Title;
    public string Preview => _note.Preview;

    /// <summary>
    /// Блоки содержимого для отображения на карточке: текст, файлы, изображения из полного контента заметки.
    /// </summary>
    public IReadOnlyList<ContentBlockItem> ContentBlocks => _contentBlocks;

    /// <summary>
    /// Только текстовые блоки — для отображения в карточке.
    /// </summary>
    public IReadOnlyList<ContentBlockItem> TextBlocks =>
        _contentBlocks.Where(b => b is TextContentBlock).ToList();

    /// <summary>
    /// Только файлы и изображения — для карусели вложений (слайдер).
    /// </summary>
    public IReadOnlyList<ContentBlockItem> AttachmentBlocks =>
        _contentBlocks.Where(b => b is FileContentBlock or ImageContentBlock).ToList();

    public bool HasAttachments => AttachmentBlocks.Count > 0;
    public bool HasTextBlocks => TextBlocks.Count > 0;
    public bool HasContentBlocks => _contentBlocks.Count > 0;

    public string TypeLabel => _note.TypeLabel;
    public string FolderName => _note.FolderName;
    public IReadOnlyList<string> TagLabels => _note.TagLabels;
    public string DateText => _note.DateText;
    public bool IsSticker => _note.IsSticker;
    public bool IsNormal => _note.IsNormal;
    public int? DurationMinutes => _note.DurationMinutes;
    public bool HasTimer => _note.HasTimer;
    public string TimerDisplayText => _note.TimerDisplayText;
    public bool HasDeadline => _note.HasDeadline;
    public string DeadlineDisplayText => _note.HasDeadline ? "До " + _note.DeadlineDisplayText : "";

    public ICommand EditCommand { get; }
    public ICommand OpenStickerCommand { get; }
    public ICommand StartTimerCommand { get; }
    public ICommand PauseTimerCommand { get; }
}
