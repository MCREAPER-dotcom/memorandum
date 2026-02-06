using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Memorandum.Desktop.Converters;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;
using Memorandum.Desktop.ViewModels;
using Memorandum.Desktop.Views;

namespace Memorandum.Desktop;

public partial class MainWindow : Window, IModalOverlayHost
{
    private readonly MainViewModel _mainVm;
    private readonly NotesListView _notesListView;
    private GraphView? _graphView;
    private readonly PresetsView _presetsView;
    private readonly SettingsView _settingsView;
    private readonly NoteEditView _noteEditView;
    private Button? _selectedNavButton;
    private readonly Dictionary<Guid, List<StickerWindow>> _openStickers = new();
    private static readonly TagNameToBrushConverter TagBrushConverter = new();
    private IntPtr _mainWindowHandle;
    private readonly HashSet<string> _remindedDeadlineKeys = new();
    private DispatcherTimer? _deadlineReminderTimer;
    private TagNameDialog? _tagDialog;
    private SubfolderNameDialog? _folderDialog;
    private PlaceholderDialog? _placeholderDialog;
    private DeadlineReminderWindow? _deadlineReminderWindow;
    private readonly IFolderTagCreationService _folderTagService;

    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;
        TrySetWindowIcon();
        _mainVm = new MainViewModel();
        _mainVm.ShowNoteEditRequested = note => ShowNoteEdit(note);
        _mainVm.ShowNoteEditWithPresetRequested = preset => ShowNoteEdit(null, preset);
        _mainVm.OpenStickerRequested = OpenSticker;
        _mainVm.CloseStickerRequested = CloseStickerForNote;
        _mainVm.ShowSubfolderDialogRequested = ShowSubfolderDialog;
        _mainVm.RefreshSidebarRequested = RefreshSidebarFromViewModel;

        var folderTagHandler = new FolderTagCreationHandler(
            title => { EnsureFolderDialog(); return _folderDialog!.ShowModalReusableAsync(this, title); },
            title => { EnsureTagDialog(); return _tagDialog!.ShowModalReusableAsync(this, title); },
            name => _mainVm.AddRootFolder(name),
            (parentPath, name) => _mainVm.AddSubfolderWithName(parentPath, name),
            (name, colorKey) => _mainVm.AddKnownTag(name, colorKey),
            RefreshSidebarFromViewModel);
        _folderTagService = new FolderTagCreationService(folderTagHandler);

        _notesListView = new NotesListView { DataContext = _mainVm.NotesList };
        _presetsView = new PresetsView
        {
            OnBack = () => _mainVm.NavigateToNotesCommand.Execute(null),
            OnApplyPresetRequested = p => ShowNoteEdit(null, p),
            FolderTagCreationService = _folderTagService,
            GetFoldersForPreset = () => _mainVm.GetFoldersForEdit(),
            GetTagNamesForPreset = () => _mainVm.GetTagNamesForEdit(),
            GetTagColorKeys = () => _mainVm.GetTagColorKeys(),
            RefreshPresetFormRequested = () =>
            {
                RefreshSidebarFromViewModel();
                _presetsView.RefreshPresetFormFolders();
                _presetsView.RefreshPresetFormTags();
            }
        };
        _settingsView = new SettingsView
        {
            OnBack = () => _mainVm.NavigateToNotesCommand.Execute(null),
            OnHotkeysSaved = RegisterAppHotkeys
        };
        _noteEditView = new NoteEditView
        {
            OnBack = () => { ContentArea.Content = GetViewForPage(); SelectNavButton(AllNotesButton); },
            OnTagCreated = (name, colorKey) => { _mainVm.AddKnownTag(name, colorKey); RefreshSidebarFromViewModel(); },
            FolderTagCreationService = _folderTagService
        };

        DataContext = _mainVm;
        LoadSidebarIcons();
        RefreshSidebarFromViewModel();
        _mainVm.PropertyChanged += OnMainVmPropertyChanged;
        SelectNavButton(AllNotesButton);
        ContentArea.Content = _notesListView;

        Opened += OnMainWindowOpened;
        Closed += OnMainWindowClosed;
        StartDeadlineReminderTimer();

        EnsureTagDialog();
        EnsureFolderDialog();
    }

    private void StartDeadlineReminderTimer()
    {
        _deadlineReminderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _deadlineReminderTimer.Tick += OnDeadlineReminderTick;
        _deadlineReminderTimer.Start();
    }

    private void OnDeadlineReminderTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        foreach (var note in _mainVm.NotesList.GetAllNotes())
        {
            if (!note.Deadline.HasValue || note.Deadline.Value > now)
                continue;
            var key = note.Title + "|" + (note.Content?.Length ?? 0) + "|" + note.Deadline.Value.Ticks;
            if (_remindedDeadlineKeys.Contains(key))
                continue;
            _remindedDeadlineKeys.Add(key);
            Dispatcher.UIThread.Post(() => ShowDeadlineReminder(note.Title, note.DeadlineDisplayText));
        }
    }

    private void ShowDeadlineReminder(string noteTitle, string deadlineText)
    {
        EnsureDeadlineReminderWindow();
        _deadlineReminderWindow!.SetNote(noteTitle, deadlineText);
        _deadlineReminderWindow.Show(this);
    }

    private void OnMainWindowOpened(object? sender, EventArgs e)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;
        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            _mainWindowHandle = handle;
            Win32ScreenshotHotkey.TryRegister(handle, () =>
                Dispatcher.UIThread.Post(CaptureScreenToClipboardAsync));
            RegisterAppHotkeys();
        }

        var preloadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        preloadTimer.Tick += (_, _) =>
        {
            preloadTimer.Stop();
            EnsureTagDialog();
            EnsureFolderDialog();
            EnsurePlaceholderDialog();
            EnsureDeadlineReminderWindow();
            WarmUpFolderAndTagDialogs();
        };
        preloadTimer.Start();
    }

    /// <summary>Первый Show() создаёт нативное окно и раскладку — вызываем его заранее в свёрнутом виде и скрываем.</summary>
    private void WarmUpFolderAndTagDialogs()
    {
        if (_folderDialog != null)
        {
            _folderDialog.WindowState = WindowState.Minimized;
            _folderDialog.Show(this);
            _folderDialog.Hide();
        }
        if (_tagDialog != null)
        {
            _tagDialog.WindowState = WindowState.Minimized;
            _tagDialog.Show(this);
            _tagDialog.Hide();
        }
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        _deadlineReminderTimer?.Stop();
        _deadlineReminderTimer = null;
        if (_mainWindowHandle != IntPtr.Zero)
        {
            Win32ScreenshotHotkey.Unregister(_mainWindowHandle);
            Win32GlobalHotkeyService.UnregisterAll(_mainWindowHandle);
            _mainWindowHandle = IntPtr.Zero;
        }
    }

    private void RegisterAppHotkeys()
    {
        if (_mainWindowHandle == IntPtr.Zero) return;
        var config = HotkeyConfigStorage.Load();
        var actions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
        {
            ["LaunchApp"] = () => Dispatcher.UIThread.Post(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }),
            ["LaunchNote"] = () => Dispatcher.UIThread.Post(() => ShowNoteEdit(null, null)),
            ["HideShowNote"] = () => Dispatcher.UIThread.Post(() =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                    Show();
                    Activate();
                }
                else
                    WindowState = WindowState.Minimized;
            }),
            ["LaunchPreset"] = () => Dispatcher.UIThread.Post(() => ShowNoteEdit(null, null))
        };
        Win32GlobalHotkeyService.UnregisterAll(_mainWindowHandle);
        Win32GlobalHotkeyService.RegisterAll(_mainWindowHandle, config, actions);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        var src = e.Source as Control;
        while (src != null)
        {
            if (src is Button || src is TextBox)
                return;
            src = src.Parent as Control;
        }
        BeginMoveDrag(e);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        if (_mainVm.CurrentPage != MainPageKind.Notes)
            _mainVm.NavigateToNotesCommand.Execute(null);
        _mainVm.NotesList.SearchAndHighlight(SearchBox.Text?.Trim() ?? "");
        _notesListView.ScrollToTop();
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnMainVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.CurrentPage)) return;
        ContentArea.Content = GetViewForPage();
        if (_mainVm.CurrentPage == MainPageKind.Notes) SelectNavButton(AllNotesButton);
        else if (_mainVm.CurrentPage == MainPageKind.Graph) SelectNavButton(GraphButton);
    }

    private Control GetViewForPage()
    {
        return _mainVm.CurrentPage switch
        {
            MainPageKind.Notes => _notesListView,
            MainPageKind.Graph => GetOrCreateGraphView(),
            MainPageKind.Presets => _presetsView,
            MainPageKind.Settings => _settingsView,
            _ => _notesListView
        };
    }

    private Control GetOrCreateGraphView()
    {
        if (_graphView == null)
        {
            _graphView = new GraphView(new NotesGraphDataProvider(() => _mainVm.NotesList.GetAllNotes()));
            _graphView.OnBack = () => _mainVm.NavigateToNotesCommand.Execute(null);
            _graphView.OnOpenNoteRequested = (titleOrId, _) =>
            {
                if (titleOrId.StartsWith("n_") && int.TryParse(titleOrId.AsSpan(2), out var idx))
                {
                    var notes = _mainVm.NotesList.GetAllNotes();
                    if (idx >= 0 && idx < notes.Count) { ShowNoteEdit(notes[idx]); return; }
                }
                OpenNoteInInterface(titleOrId, "Содержимое заметки");
            };
        }
        _graphView.RefreshData();
        return _graphView;
    }

    private void EnsureFolderDialog()
    {
        if (_folderDialog == null)
            _folderDialog = new SubfolderNameDialog();
    }

    private void EnsureTagDialog()
    {
        if (_tagDialog == null)
            _tagDialog = new TagNameDialog();
    }

    private void EnsurePlaceholderDialog()
    {
        if (_placeholderDialog == null)
            _placeholderDialog = new PlaceholderDialog();
    }

    private void EnsureDeadlineReminderWindow()
    {
        if (_deadlineReminderWindow == null)
            _deadlineReminderWindow = new DeadlineReminderWindow();
    }

    void IModalOverlayHost.SetModalOverlayVisible(bool visible)
    {
        ModalOverlay.IsVisible = visible;
    }

    private async Task<TagCreationResult?> ShowTagDialogAsync(string title)
    {
        EnsureTagDialog();
        return await _tagDialog!.ShowModalReusableAsync(this, title);
    }

    private async Task<string?> ShowSubfolderDialog()
    {
        EnsureFolderDialog();
        return await _folderDialog!.ShowModalReusableAsync(this, "Вложенная папка");
    }

    private void RefreshSidebarFromViewModel()
    {
        _mainVm.RefreshFolderItems();
        _mainVm.RefreshTagItems();
        FoldersList.Children.Clear();
        foreach (var item in _mainVm.FolderItems)
        {
            var depth = item.Depth;
            var icon = new Image { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center };
            SetSvgIcon(icon, "avares://Memorandum.Desktop/Assets/Icons/folders.svg");
            var label = new TextBlock { Text = $"{item.DisplayName} ({item.Count})", VerticalAlignment = VerticalAlignment.Center };
            var folderPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(depth * 12, 0, 0, 0), Children = { icon, label } };
            var btn = new Button { Content = folderPanel, Classes = { "NavItem" }, Command = item.SelectCommand };
            if (item.IsSelected) btn.Classes.Add("Selected");
            var plusIcon = new Image { Width = 14, Height = 14, VerticalAlignment = VerticalAlignment.Center };
            SetSvgIcon(plusIcon, "avares://Memorandum.Desktop/Assets/Icons/plus.svg");
            var plusBtn = new Button { Content = plusIcon, Command = item.AddSubfolderCommand, Padding = new Thickness(4), Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { btn, plusBtn } };
            FoldersList.Children.Add(row);
        }
        TagsList.Children.Clear();
        foreach (var item in _mainVm.TagItems)
        {
            var brush = GetTagBrush(item);
            var dot = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(6), Background = brush, VerticalAlignment = VerticalAlignment.Center };
            var label = new TextBlock { Text = $"{item.Name} ({item.Count})", VerticalAlignment = VerticalAlignment.Center };
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { dot, label } };
            var btn = new Button { Content = panel, Classes = { "NavItem" }, Command = item.SelectCommand };
            if (item.IsSelected) btn.Classes.Add("Selected");
            TagsList.Children.Add(btn);
        }

        if (ContentArea.Content == _noteEditView)
        {
            _noteEditView.SetFolders(_mainVm.GetFoldersForEdit());
            _noteEditView.SetAvailableTags(_mainVm.GetTagNamesForEdit(), _mainVm.GetTagColorKeys());
        }
    }

    private void TrySetWindowIcon()
    {
        if (AppIconCache.Icon != null)
            Icon = AppIconCache.Icon;
    }

    private void LoadSidebarIcons()
    {
        // Для абсолютных avares:// путей baseUri не нужен
        SetSvgIcon(IconAllNotes, "avares://Memorandum.Desktop/Assets/Icons/document.svg");
        SetSvgIcon(IconGraph, "avares://Memorandum.Desktop/Assets/Icons/diagram.svg");
        SetSvgIcon(IconFolders, "avares://Memorandum.Desktop/Assets/Icons/folders.svg");
        SetSvgIcon(IconTags, "avares://Memorandum.Desktop/Assets/Icons/brand.svg");
    }

    private static IBrush GetTagBrush(ViewModels.TagItemViewModel item)
    {
        if (!string.IsNullOrEmpty(item.ColorKey) && Avalonia.Application.Current?.Resources?.TryGetResource(item.ColorKey, null, out var value) == true && value is IBrush b)
            return b;
        return TagBrushConverter.Convert(item.Name, typeof(IBrush), null, CultureInfo.CurrentCulture) as IBrush ?? Brushes.Gray;
    }

    private static void SetSvgIcon(Image imageControl, string avaresPath)
    {
        try
        {
            var source = SvgSource.Load(avaresPath, null);
            if (source != null)
                imageControl.Source = new SvgImage { Source = source };
        }
        catch
        {
            // Иконка не загружена — оставляем пустое место
        }
    }

    private void SelectNavButton(Button button)
    {
        if (_selectedNavButton != null)
            _selectedNavButton.Classes.Remove("Selected");
        _selectedNavButton = button;
        button.Classes.Add("Selected");
    }

    private void OnScreenshotClick(object? sender, RoutedEventArgs e) =>
        CaptureScreenToClipboardAsync();

    private void ShowNoteEdit(NoteCardItem? item, PresetItem? preset = null)
    {
        _noteEditView.SetFolders(_mainVm.GetFoldersForEdit());
        _noteEditView.SetAvailableTags(_mainVm.GetTagNamesForEdit(), _mainVm.GetTagColorKeys());
        _noteEditView.SetNote(item, preset);
        _noteEditView.OnSaveRequested = (title, description, content, folder, tags, isSticker, editingNote, durationMinutes, backgroundColorHex, transparencyPercent, isClickThrough, isPinned, closeOnTimerEnd, deadline) =>
        {
            var preview = description ?? "";
            var typeLabel = isSticker ? "Стикер" : "Обычная";
            var now = DateTime.UtcNow;
            NoteCardItem? newItem = null;
            newItem = new NoteCardItem(
                title, preview, content, typeLabel, folder ?? "", tags, isSticker,
                () => ShowNoteEdit(newItem!),
                () => OpenSticker(newItem!),
                durationMinutes,
                () => OpenSticker(newItem!),
                () => CloseStickerForNote(newItem!),
                backgroundColorHex, transparencyPercent, isClickThrough, isPinned, closeOnTimerEnd, deadline,
                id: editingNote?.Id,
                createdAt: editingNote?.CreatedAt,
                lastEditedAt: now);
            if (editingNote != null)
                _mainVm.NotesList.ReplaceNote(editingNote, newItem);
            else
                _mainVm.NotesList.AddNote(newItem);
            ContentArea.Content = _notesListView;
            _mainVm.NotesList.RefreshFilter(null, null);
            RefreshSidebarFromViewModel();
        };
        _noteEditView.OnDeleteRequested = (note) =>
        {
            CloseStickerForNote(note);
            _mainVm.NotesList.RemoveNote(note);
            ContentArea.Content = _notesListView;
            _mainVm.NotesList.RefreshFilter(null, null);
            RefreshSidebarFromViewModel();
        };
        ContentArea.Content = _noteEditView;
    }

    private async void OnAddFolderClick(object? sender, RoutedEventArgs e)
    {
        EnsureFolderDialog();
        var name = await _folderDialog!.ShowModalReusableAsync(this, "Добавить папку");
        if (!string.IsNullOrWhiteSpace(name))
        {
            _mainVm.AddRootFolder(name!);
            RefreshSidebarFromViewModel();
        }
    }

    private async void OnAddTagClick(object? sender, RoutedEventArgs e)
    {
        var result = await ShowTagDialogAsync("Добавить тег");
        if (result != null)
        {
            _mainVm.AddKnownTag(result.Name, result.ColorKey);
            RefreshSidebarFromViewModel();
        }
    }

    private void OpenSticker(NoteCardItem note)
    {
        var noteId = note.Id;
        if (!_openStickers.TryGetValue(noteId, out var list))
        {
            list = new List<StickerWindow>();
            _openStickers[noteId] = list;
        }
        var sticker = new StickerWindow(note.Title, note.Content, note.Preview, note.DurationMinutes, note.BackgroundColorHex, note.TransparencyPercent, note.IsClickThrough, note.IsPinned, note.CloseOnTimerEnd, note.Deadline);
        if (Icon != null)
            sticker.Icon = Icon;
        sticker.OnContentSaved = (newPreview) =>
        {
            var newItem = CreateReplacementNoteWithPreview(note, newPreview);
            _mainVm.NotesList.ReplaceNote(note, newItem);
        };
        sticker.Closed += (_, _) =>
        {
            if (_openStickers.TryGetValue(noteId, out var lst) && lst.Remove(sticker) && lst.Count == 0)
                _openStickers.Remove(noteId);
        };
        list.Add(sticker);
        sticker.Show();
    }

    private NoteCardItem CreateReplacementNoteWithContent(NoteCardItem oldNote, string newContent)
    {
        NoteCardItem? newItem = null;
        newItem = new NoteCardItem(
            oldNote.Title, oldNote.Preview, newContent, oldNote.TypeLabel, oldNote.FolderName, oldNote.TagLabels, oldNote.IsSticker,
            () => ShowNoteEdit(newItem!),
            () => OpenSticker(newItem!),
            oldNote.DurationMinutes,
            () => OpenSticker(newItem!),
            () => CloseStickerForNote(newItem!),
            oldNote.BackgroundColorHex, oldNote.TransparencyPercent, oldNote.IsClickThrough, oldNote.IsPinned, oldNote.CloseOnTimerEnd, oldNote.Deadline,
            id: oldNote.Id,
            createdAt: oldNote.CreatedAt,
            lastEditedAt: DateTime.UtcNow);
        return newItem;
    }

    private NoteCardItem CreateReplacementNoteWithPreview(NoteCardItem oldNote, string newPreview)
    {
        NoteCardItem? newItem = null;
        newItem = new NoteCardItem(
            oldNote.Title, newPreview, oldNote.Content, oldNote.TypeLabel, oldNote.FolderName, oldNote.TagLabels, oldNote.IsSticker,
            () => ShowNoteEdit(newItem!),
            () => OpenSticker(newItem!),
            oldNote.DurationMinutes,
            () => OpenSticker(newItem!),
            () => CloseStickerForNote(newItem!),
            oldNote.BackgroundColorHex, oldNote.TransparencyPercent, oldNote.IsClickThrough, oldNote.IsPinned, oldNote.CloseOnTimerEnd, oldNote.Deadline,
            id: oldNote.Id,
            createdAt: oldNote.CreatedAt,
            lastEditedAt: DateTime.UtcNow);
        return newItem;
    }

    private void CloseStickerForNote(NoteCardItem note)
    {
        if (!_openStickers.TryGetValue(note.Id, out var list))
            return;
        foreach (var window in list.ToList())
            window.Close();
        _openStickers.Remove(note.Id);
    }

    private void OpenNoteInInterface(string title, string content)
    {
        NoteCardItem? tempItem = null;
        tempItem = new NoteCardItem(
            title, content, content, "Обычная", "", Array.Empty<string>(), false,
            () => ShowNoteEdit(tempItem!),
            () => { },
            null,
            () => { },
            () => { },
            null, 100, false, true, false, null);
        ShowNoteEdit(tempItem);
    }

    private async Task ShowPlaceholder(string title, string message)
    {
        EnsurePlaceholderDialog();
        await _placeholderDialog!.ShowReusableAsync(this, title, message);
    }

    private void CaptureScreenToClipboardAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ = ShowPlaceholder("Скриншот", "Захват экрана недоступен на этой платформе.");
            return;
        }

        var (x, y, w, h) = ScreenshotClipboardService.GetVirtualScreenBounds();
        var overlay = new ScreenshotOverlayWindow
        {
            Position = new PixelPoint(x, y),
            Width = w,
            Height = h,
            ShowInTaskbar = false
        };
        overlay.Show();
    }
}
