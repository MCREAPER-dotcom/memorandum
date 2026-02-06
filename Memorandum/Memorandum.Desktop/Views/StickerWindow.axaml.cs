using System.Diagnostics;
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
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Memorandum.Desktop;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;
using Memorandum.Desktop.Themes;
using NAudio.Wave;

namespace Memorandum.Desktop.Views;

public partial class StickerWindow : Window, IContentBlockRemover
{
    private DispatcherTimer? _countdownTimer;
    private int _remainingSeconds;
    private readonly bool _closeOnTimerEnd;
    private int _transparencyPercent;
    private string _displayContent = "";
    private string _editPreview = "";
    private string? _backgroundColorHex;
    private bool _isClickThroughMode;

    public Action<string>? OnContentSaved { get; set; }

    public StickerWindow()
    {
        InitializeComponent();
        _transparencyPercent = 100;
        ApplyCachedIcon();
        DragDrop.SetAllowDrop(StickerEditDropZone, true);
        DragDrop.SetAllowDrop(EditPanel, true);
        StickerEditDropZone.AddHandler(DragDrop.DragOverEvent, OnStickerDragOver);
        StickerEditDropZone.AddHandler(DragDrop.DropEvent, OnStickerDrop);
        StickerEditDropZone.PointerPressed += OnStickerDropZonePointerPressed;
        EditPanel.AddHandler(DragDrop.DragOverEvent, OnStickerDragOver);
        EditPanel.AddHandler(DragDrop.DropEvent, OnStickerDrop);
        Opened += OnStickerOpened;
        Closed += (_, _) =>
        {
            _countdownTimer?.Stop();
            _countdownTimer = null;
        };
    }

    private void OnStickerOpened(object? sender, EventArgs e)
    {
        LoadClipIcon();
        UpdateClipButtonVisual();
        if (_isClickThroughMode)
            ApplyClickThroughMode();
    }

    private void LoadClipIcon()
    {
        if (ClipButtonIcon == null) return;
        if (Application.Current?.Resources.TryGetResource("ClipIcon", null, out var icon) == true && icon is Avalonia.Media.IImage img)
            ClipButtonIcon.Source = img;
    }

    private void OnClipButtonClick(object? sender, RoutedEventArgs e)
    {
        _isClickThroughMode = !_isClickThroughMode;
        ApplyClickThroughMode();
        UpdateClipButtonVisual();
    }

    private void UpdateClipButtonVisual()
    {
        if (ClipButton == null) return;
        if (_isClickThroughMode)
        {
            ClipButton.Background = new SolidColorBrush(Color.FromArgb(220, 100, 140, 200));
            ClipButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 80, 120, 180));
        }
        else
        {
            ClipButton.Background = Brushes.Transparent;
            ClipButton.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 160, 160, 160));
        }
    }

    private void ApplyClickThroughMode()
    {
        if (!OperatingSystem.IsWindows()) return;
        TrySetClickThrough(_isClickThroughMode);
    }

    private void ApplyCachedIcon()
    {
        if (AppIconCache.Icon != null)
            Icon = AppIconCache.Icon;
    }

    public StickerWindow(string title, string contentForDisplay, string previewForEdit, int? durationMinutes = null, string? backgroundColorHex = null,
        int transparencyPercent = 100, bool isClickThrough = false, bool isPinned = true, bool closeOnTimerEnd = false, DateTime? deadline = null) : this()
    {
        _closeOnTimerEnd = closeOnTimerEnd;
        _transparencyPercent = Math.Clamp(transparencyPercent, 1, 100);
        _displayContent = contentForDisplay ?? "";
        _editPreview = previewForEdit ?? "";
        _backgroundColorHex = backgroundColorHex;
        Title = title;
        Topmost = isPinned;
        Opacity = 1;
        BuildContentWithLinks(_displayContent);
        ApplyBackgroundColor(backgroundColorHex);
        if (deadline.HasValue)
        {
            DeadlineText.Text = "До " + deadline.Value.ToString("dd.MM.yyyy HH:mm");
            DeadlineText.IsVisible = true;
        }
        if (isClickThrough)
        {
            _isClickThroughMode = true;
            Opened += OnOpenedSetClickThrough;
        }
        if (durationMinutes.HasValue && durationMinutes.Value > 0)
        {
            _remainingSeconds = durationMinutes.Value * 60;
            TimerText.Text = FormatCountdown(_remainingSeconds);
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += OnCountdownTick;
            _countdownTimer.Start();
        }
        else
        {
            TimerText.Text = "00:00";
        }
    }

    private void OnOpenedSetClickThrough(object? sender, EventArgs e)
    {
        Opened -= OnOpenedSetClickThrough;
        ApplyClickThroughMode();
    }

    private void TrySetClickThrough(bool enable)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var handle = TryGetPlatformHandle()?.Handle ?? nint.Zero;
            if (handle == nint.Zero) return;
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TRANSPARENT = 0x00000020;
            var exStyle = (int)GetWindowLong(handle, GWL_EXSTYLE);
            if (enable)
                exStyle |= WS_EX_TRANSPARENT;
            else
                exStyle &= ~WS_EX_TRANSPARENT;
            SetWindowLong(handle, GWL_EXSTYLE, (IntPtr)exStyle);
        }
        catch
        {
            // сквозные клики не применены
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private void ApplyBackgroundColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            ApplyFallbackBackground();
            return;
        }
        var s = hex.Trim();
        if (!s.StartsWith("#")) s = "#" + s;
        if (s.Length < 4)
        {
            ApplyFallbackBackground();
            return;
        }
        try
        {
            var color = Avalonia.Media.Color.Parse(s);
            var brush = new SolidColorBrush(WithTransparency(color, _transparencyPercent));
            Background = brush;
            RootBorder.Background = brush;
        }
        catch
        {
            ApplyFallbackBackground();
        }
    }

    private void ApplyFallbackBackground()
    {
        Color baseColor = Color.Parse(PaletteConstants.DefaultStickerBackgroundHex);
        if (Avalonia.Application.Current?.Resources?.TryGetResource(PaletteConstants.StickerBackgroundKey, null, out var value) == true && value is SolidColorBrush scb)
            baseColor = scb.Color;
        var brush = new SolidColorBrush(WithTransparency(baseColor, _transparencyPercent));
        Background = brush;
        RootBorder.Background = brush;
    }

    private static Color WithTransparency(Color color, int percent)
    {
        var a = (byte)(255 * Math.Clamp(percent, 1, 100) / 100);
        return Color.FromArgb(a, color.R, color.G, color.B);
    }

    private static string FormatCountdown(int totalSeconds)
    {
        var m = totalSeconds / 60;
        var s = totalSeconds % 60;
        return $"{m:D2}:{s:D2}";
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        TimerText.Text = FormatCountdown(Math.Max(0, _remainingSeconds));
        if (_remainingSeconds <= 0)
        {
            _countdownTimer?.Stop();
            _countdownTimer = null;
            TimerText.Text = "Готово!";
            _ = System.Threading.Tasks.Task.Run(PlayNotificationSound);
            if (_closeOnTimerEnd)
                Dispatcher.UIThread.Post(Close);
        }
    }

    private static void PlayNotificationSound()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Sounds", "Notification.mp3");
            if (!File.Exists(path))
                return;
            using var reader = new Mp3FileReader(path);
            using var waveOut = new WaveOutEvent();
            waveOut.Init(reader);
            waveOut.Play();
            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        }
        catch
        {
            // звук не воспроизведён
        }
    }

    private void BuildContentWithLinks(string? content)
    {
        ContentBlocksControl.ItemsSource = ContentParser.Parse(content ?? "");
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnResizeNorth(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North, e);
    private void OnResizeSouth(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South, e);
    private void OnResizeWest(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West, e);
    private void OnResizeEast(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East, e);
    private void OnResizeNorthWest(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthWest, e);
    private void OnResizeNorthEast(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthEast, e);
    private void OnResizeSouthWest(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthWest, e);
    private void OnResizeSouthEast(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthEast, e);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.H && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                _isClickThroughMode = !_isClickThroughMode;
                ApplyClickThroughMode();
                UpdateClipButtonVisual();
                e.Handled = true;
            }
            else
            {
                e.Handled = true;
                Hide();
            }
        }
    }

    private void OnEditModeClick(object? sender, RoutedEventArgs e)
    {
        Opacity = 1;
        ApplyBackgroundColorSolid(_backgroundColorHex);
        ViewScroll.IsVisible = false;
        EditPanel.IsVisible = true;
        EditContentBlocksControl.ItemsSource = ContentParser.Parse(_displayContent ?? "");
        EditContentBox.Text = _editPreview ?? "";
        EditContentBox.Focus();
        EditModeButton.IsVisible = false;
        EditButtonsPanel.IsVisible = true;
    }

    private void OnStickerDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetFiles()?.Any() == true)
            e.DragEffects = DragDropEffects.Copy;
    }

    private void OnStickerDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data?.GetFiles();
        if (files == null) return;
        var paths = files.Select(f => f.TryGetLocalPath()).Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).ToList();
        var content = ContentInsertionService.ProcessDroppedPaths(paths);
        if (!string.IsNullOrEmpty(content))
        {
            AppendToEditContent(content);
            e.Handled = true;
        }
    }

    private void OnStickerDropZonePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _ = StickerPasteFromClipboardAsync();
    }

    private void OnStickerPasteFromClipboardClick(object? sender, RoutedEventArgs e)
    {
        _ = StickerPasteFromClipboardAsync();
    }

    private async Task StickerPasteFromClipboardAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var clipboard = topLevel?.Clipboard;
        if (clipboard == null && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            clipboard = desktop.MainWindow?.Clipboard;
        var handle = topLevel?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        Func<Task<string?>>? getText = clipboard != null ? (async () => await clipboard.GetTextAsync()) : null;
        var content = await ContentInsertionService.GetPastedContentAsync(getText, handle);
        if (!string.IsNullOrEmpty(content))
            AppendToEditContent(content);
    }

    private void AppendToEditContent(string text)
    {
        var box = EditContentBox;
        var current = box.Text ?? "";
        var start = Math.Clamp(box.SelectionStart, 0, current.Length);
        var end = Math.Clamp(box.SelectionEnd, 0, current.Length);
        var before = current.Substring(0, start);
        var after = current.Substring(end);
        box.Text = before + text + after;
        box.SelectionStart = box.SelectionEnd = start + text.Length;
    }

    public void RemoveBlock(ContentBlockItem block)
    {
        var marker = block is FileContentBlock f ? f.GetMarkerToRemove()
            : block is ImageContentBlock i ? i.GetMarkerToRemove()
            : null;
        if (string.IsNullOrEmpty(marker)) return;
        var idx = _displayContent.IndexOf(marker);
        if (idx < 0) return;
        _displayContent = _displayContent.Remove(idx, marker.Length);
        EditContentBlocksControl.ItemsSource = ContentParser.Parse(_displayContent);
    }

    private void ApplyBackgroundColorSolid(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            var color = Color.Parse(PaletteConstants.DefaultStickerBackgroundHex);
            var brush = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
            Background = brush;
            RootBorder.Background = brush;
            return;
        }
        var s = hex.Trim();
        if (!s.StartsWith("#")) s = "#" + s;
        try
        {
            var color = Avalonia.Media.Color.Parse(s);
            var brush = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
            Background = brush;
            RootBorder.Background = brush;
        }
        catch
        {
            var color = Color.Parse(PaletteConstants.DefaultStickerBackgroundHex);
            var brush = new SolidColorBrush(Color.FromArgb(255, color.R, color.G, color.B));
            Background = brush;
            RootBorder.Background = brush;
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        _editPreview = EditContentBox.Text ?? "";
        OnContentSaved?.Invoke(_editPreview);
        ShowViewMode();
    }

    private void OnCancelEditClick(object? sender, RoutedEventArgs e)
    {
        ShowViewMode();
    }

    private void ShowViewMode()
    {
        EditPanel.IsVisible = false;
        ViewScroll.IsVisible = true;
        BuildContentWithLinks(_displayContent);
        EditButtonsPanel.IsVisible = false;
        EditModeButton.IsVisible = true;
        ApplyBackgroundColor(_backgroundColorHex);
    }
}
