using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Views;

public partial class NoteEditView : UserControl
{
    private NoteCardItem? _editingNote;
    private readonly List<string> _availableTags = new();
    private readonly Dictionary<string, string> _tagColors = new();
    private readonly HashSet<string> _selectedTags = new();
    private List<FolderRowForEdit> _folderRows = new();
    private string? _selectedFolderPath;

    public NoteEditView()
    {
        InitializeComponent();
        ImageDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        ImageDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
        ImageDropZone.PointerPressed += OnImageDropZonePointerPressed;
        ContentBox.TextChanged += (_, _) => RefreshContentBlocks();
        var contentCard = ContentBox.Parent?.Parent as Border;
        if (contentCard != null)
        {
            contentCard.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            contentCard.AddHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    private void RefreshContentBlocks()
    {
        var content = ContentBox.Text ?? "";
        ContentBlocksControl.ItemsSource = ContentParser.Parse(content);
    }

    public System.Action? OnBack { get; set; }

    public System.Action<string, string, string, string?, IReadOnlyList<string>, bool, NoteCardItem?, int?, string?, int, bool, bool, bool, DateTime?>? OnSaveRequested { get; set; }
    public System.Action<NoteCardItem>? OnDeleteRequested { get; set; }
    public System.Action<string, string?>? OnTagCreated { get; set; }
    public Func<System.Threading.Tasks.Task>? OnAddFolderRequested { get; set; }
    public Func<string, System.Threading.Tasks.Task>? OnAddSubfolderRequested { get; set; }
    public Func<string, Task<TagCreationResult?>>? ShowTagDialogAsync { get; set; }

    public void SetFolders(IReadOnlyList<FolderRowForEdit> folders)
    {
        _folderRows = folders?.ToList() ?? new List<FolderRowForEdit>();
        FolderPanel.Children.Clear();
        foreach (var row in _folderRows)
        {
            var path = row.Path;
            var folderBtn = new Button
            {
                Content = $"{row.DisplayName} ({row.Count})",
                Tag = path,
                Classes = { "NavItem" },
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Thickness(row.Depth * 14, 0, 0, 0),
                Padding = new Thickness(10, 6)
            };
            if (Avalonia.Application.Current?.Resources?.TryGetResource("PrimaryForeground", null, out var fgBrush) == true && fgBrush is IBrush fb)
                folderBtn.Foreground = fb;
            folderBtn.Click += (_, _) =>
            {
                _selectedFolderPath = path;
                ApplyFolderSelection();
            };
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            rowPanel.Children.Add(folderBtn);
            if (OnAddSubfolderRequested != null)
            {
                var plusBtn = new Button
                {
                    Content = "+",
                    Tag = path,
                    Padding = new Thickness(4),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 14
                };
                if (Avalonia.Application.Current?.Resources?.TryGetResource("PrimaryForeground", null, out var fg) == true && fg is IBrush brush)
                    plusBtn.Foreground = brush;
                plusBtn.Click += async (_, _) =>
                {
                    if (plusBtn.Tag is string parentPath)
                        await (OnAddSubfolderRequested?.Invoke(parentPath) ?? System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(true);
                };
                rowPanel.Children.Add(plusBtn);
            }
            FolderPanel.Children.Add(rowPanel);
        }
        ApplyFolderSelection();
    }

    private async void OnAddFolderClick(object? sender, RoutedEventArgs e)
    {
        await (OnAddFolderRequested?.Invoke() ?? System.Threading.Tasks.Task.CompletedTask).ConfigureAwait(true);
    }

    public void SetAvailableTags(IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? colorKeysByTag = null)
    {
        _availableTags.Clear();
        if (tagNames != null)
            _availableTags.AddRange(tagNames);
        for (var i = 0; i < _availableTags.Count; i++)
        {
            var name = _availableTags[i];
            if (_tagColors.ContainsKey(name)) continue;
            if (colorKeysByTag != null && colorKeysByTag.TryGetValue(name, out var storedKey) && !string.IsNullOrEmpty(storedKey))
                _tagColors[name] = storedKey;
            else if (PaletteConstants.DefaultTagNameToKey.TryGetValue(name, out var k))
                _tagColors[name] = k;
            else
                _tagColors[name] = PaletteConstants.TagPillResourceKeys[Math.Abs(name.GetHashCode()) % PaletteConstants.TagPillResourceKeys.Length];
        }
        BuildTagsPanel();
    }

    private void ApplyFolderSelection()
    {
        foreach (var child in FolderPanel.Children)
        {
            if (child is StackPanel row && row.Children.Count > 0 && row.Children[0] is Button b && b.Tag is string path)
            {
                if (string.Equals(path, _selectedFolderPath, StringComparison.OrdinalIgnoreCase))
                    b.Classes.Add("Selected");
                else
                    b.Classes.Remove("Selected");
            }
        }
    }

    public void SetNote(NoteCardItem? note, PresetItem? preset = null)
    {
        _editingNote = note;
        _selectedTags.Clear();
        if (note != null)
        {
            TitleText.Text = "Редактирование заметки";
            TitleBox.Text = note.Title;
            DescriptionBox.Text = note.Preview ?? "";
            ContentBox.Text = note.Content;
            RefreshContentBlocks();
            DeleteButton.IsVisible = true;
            _selectedFolderPath = note.FolderName;
            ApplyFolderSelection();
            foreach (var t in note.TagLabels)
                _selectedTags.Add(t);
            TypeSticker.IsChecked = note.IsSticker;
            TypeNormal.IsChecked = note.IsNormal;
            TimerDurationBox.Text = note.DurationMinutes?.ToString() ?? "";
            StickerBackgroundBox.Text = note.BackgroundColorHex;
            StickerTransparencyBox.Text = note.TransparencyPercent.ToString();
            StickerClickThroughCheck.IsChecked = note.IsClickThrough;
            StickerPinnedCheck.IsChecked = note.IsPinned;
            StickerCloseOnTimerCheck.IsChecked = note.CloseOnTimerEnd;
            DeadlinePickerControl.SetDeadline(note.Deadline);
        }
        else
        {
            TitleText.Text = "Создание заметки";
            TitleBox.Text = preset != null ? preset.Title : "";
            DescriptionBox.Text = "";
            ContentBox.Text = "";
            RefreshContentBlocks();
            DeleteButton.IsVisible = false;
            _selectedFolderPath = null;
            ApplyFolderSelection();
            TimerDurationBox.Text = preset?.DurationMinutes?.ToString() ?? "";
            StickerBackgroundBox.Text = "";
            StickerTransparencyBox.Text = "100";
            StickerClickThroughCheck.IsChecked = false;
            StickerPinnedCheck.IsChecked = true;
            DeadlinePickerControl.SetDeadline(null);
            if (preset != null)
            {
                TypeSticker.IsChecked = preset.IsSticker;
                TypeNormal.IsChecked = !preset.IsSticker;
                foreach (var tag in preset.TagLabels)
                {
                    if (!_availableTags.Contains(tag))
                        _availableTags.Add(tag);
                    _selectedTags.Add(tag);
                }
            }
        }
        BuildTagsPanel();
    }

    private static IBrush ResolveTagBrush(string colorKey)
    {
        if (Avalonia.Application.Current?.Resources?.TryGetResource(colorKey, null, out var value) == true && value is IBrush brush)
            return brush;
        return new SolidColorBrush(Color.Parse(PaletteConstants.DefaultTagPillFallbackHex));
    }

    private void BuildTagsPanel()
    {
        TagsWrap.Children.Clear();
        for (var i = 0; i < _availableTags.Count; i++)
        {
            var name = _availableTags[i];
            var colorKey = _tagColors.TryGetValue(name, out var c) ? c
                : (PaletteConstants.DefaultTagNameToKey.TryGetValue(name, out var k) ? k : PaletteConstants.TagPillResourceKeys[i % PaletteConstants.TagPillResourceKeys.Length]);
            IBrush brush = ResolveTagBrush(colorKey);
            var border = new Border
            {
                Tag = name,
                Margin = new Thickness(0, 0, 6, 6),
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(6),
                Background = brush,
                BorderThickness = new Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = name,
                    FontSize = 12,
                    Foreground = this.TryFindResource("PrimaryForeground", out var fr) && fr is IBrush fb ? fb : Brushes.White,
                    IsHitTestVisible = false
                }
            };
            ApplyTagSelectedStyle(border, _selectedTags.Contains(name));
            border.PointerPressed += OnTagPointerPressed;
            border.PointerEntered += (_, _) => border.Classes.Add("TagHover");
            border.PointerExited += (_, _) => border.Classes.Remove("TagHover");
            TagsWrap.Children.Add(border);
        }
    }

    private void OnTagPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var border = sender as Border ?? (sender as Control)?.Parent as Border;
        if (border?.Tag is not string name)
            return;
        if (_selectedTags.Contains(name))
            _selectedTags.Remove(name);
        else
            _selectedTags.Add(name);
        var selected = _selectedTags.Contains(name);
        if (selected)
            border.Classes.Add("TagSelected");
        else
            border.Classes.Remove("TagSelected");
        ApplyTagSelectedStyle(border, selected);
    }

    private static void ApplyTagSelectedStyle(Border border, bool selected)
    {
        if (selected)
        {
            border.BorderThickness = new Thickness(3);
            border.BorderBrush = Brushes.White;
        }
        else
        {
            border.BorderThickness = new Thickness(0);
            border.BorderBrush = null;
        }
    }

    private void OnBackClick(object? sender, RoutedEventArgs e) => OnBack?.Invoke();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => OnBack?.Invoke();

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var title = (TitleBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(title))
        {
            TitleBox.Focus();
            return;
        }
        var description = DescriptionBox.Text ?? "";
        var content = ContentBox.Text ?? "";
        var folder = _selectedFolderPath;
        var tags = _selectedTags.ToList();
        var isSticker = TypeSticker.IsChecked == true;
        int? durationMinutes = null;
        if (!string.IsNullOrWhiteSpace(TimerDurationBox.Text) && int.TryParse(TimerDurationBox.Text.Trim(), out var d) && d > 0)
            durationMinutes = d;
        var bgHex = (StickerBackgroundBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(bgHex)) bgHex = null;
        if (!int.TryParse((StickerTransparencyBox.Text ?? "100").Trim(), out var transparency) || transparency < 1 || transparency > 100)
            transparency = 100;
        var isClickThrough = StickerClickThroughCheck.IsChecked == true;
        var isPinned = StickerPinnedCheck.IsChecked == true;
        var closeOnTimer = StickerCloseOnTimerCheck.IsChecked == true;
        DateTime? deadline = DeadlinePickerControl.GetDeadline();
        OnSaveRequested?.Invoke(title, description, content, folder, tags, isSticker, _editingNote, durationMinutes, bgHex, transparency, isClickThrough, isPinned, closeOnTimer, deadline);
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_editingNote != null)
            OnDeleteRequested?.Invoke(_editingNote);
        OnBack?.Invoke();
    }

    private void OnAddLinkClick(object? sender, RoutedEventArgs e) { }

    private void OnImageDropZonePointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        _ = PasteFromClipboardAsync();
    }

    private void OnPasteFromClipboardClick(object? sender, RoutedEventArgs e)
    {
        _ = PasteFromClipboardAsync();
    }

    private async System.Threading.Tasks.Task PasteFromClipboardAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var clipboard = topLevel?.Clipboard;
        if (clipboard == null && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            clipboard = desktop.MainWindow?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.GetTextAsync();
        if (!string.IsNullOrEmpty(text))
        {
            AppendToContent(text);
            RefreshContentBlocks();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && topLevel != null)
        {
            var handle = topLevel.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle != IntPtr.Zero)
            {
                var imagePath = ScreenshotClipboardService.GetImageFromClipboardAndSaveToFile(handle);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    AppendToContent(Environment.NewLine + "[Изображение: " + imagePath + "]" + Environment.NewLine);
                    RefreshContentBlocks();
                }
            }
        }
    }

    private static string? SaveBitmapToAttachments(Avalonia.Media.Imaging.Bitmap bitmap)
    {
        var dir = NoteAttachmentsHelper.GetAttachmentsFolder();
        var fileName = $"{Guid.NewGuid():N}.png";
        var path = Path.Combine(dir, fileName);
        try
        {
            bitmap.Save(path);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private void AppendToContent(string text)
    {
        var current = ContentBox.Text ?? "";
        var start = Math.Clamp(ContentBox.SelectionStart, 0, current.Length);
        var end = Math.Clamp(ContentBox.SelectionEnd, 0, current.Length);
        var before = current.Substring(0, start);
        var after = current.Substring(end);
        ContentBox.Text = before + text + after;
        ContentBox.SelectionStart = ContentBox.SelectionEnd = start + text.Length;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetFiles()?.Any() == true)
            e.DragEffects = DragDropEffects.Copy;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data?.GetFiles();
        if (files != null)
        {
            var lines = new List<string>();
            foreach (var item in files)
            {
                var path = item.TryGetLocalPath();
                if (string.IsNullOrEmpty(path)) continue;
                if (Directory.Exists(path))
                    lines.Add("[Папка: " + path + "]");
                else
                {
                    var copied = NoteAttachmentsHelper.CopyToAttachments(path);
                    lines.Add(copied != null ? "[Файл: " + copied + "]" : "[Файл: " + path + "]");
                }
            }
            if (lines.Count > 0)
            {
                AppendToContent(Environment.NewLine + string.Join(Environment.NewLine, lines) + Environment.NewLine);
                RefreshContentBlocks();
                e.Handled = true;
            }
        }
    }

    private async void OnCreateTagClick(object? sender, RoutedEventArgs e)
    {
        TagCreationResult? result;
        if (ShowTagDialogAsync != null)
        {
            result = await ShowTagDialogAsync("Новый тег");
        }
        else
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;
            var dlg = new TagNameDialog { Title = "Новый тег" };
            result = await dlg.ShowDialog<TagCreationResult?>(owner);
        }
        if (result == null || string.IsNullOrWhiteSpace(result.Name))
            return;
        var name = result.Name.Trim();
        if (_availableTags.Contains(name))
            return;
        _availableTags.Add(name);
        _tagColors[name] = result.ColorKey;
        BuildTagsPanel();
        OnTagCreated?.Invoke(name, result.ColorKey);
    }
}
