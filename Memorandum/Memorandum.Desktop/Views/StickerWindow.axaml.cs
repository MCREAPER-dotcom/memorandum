using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;
using Memorandum.Desktop.Themes;
using NAudio.Wave;

namespace Memorandum.Desktop.Views;

public partial class StickerWindow : Window
{
    private static readonly Regex UrlRegex = new(@"https?://[^\s<>""]+|www\.[^\s<>""]+", RegexOptions.Compiled);
    private DispatcherTimer? _countdownTimer;
    private int _remainingSeconds;
    private readonly bool _closeOnTimerEnd;
    private int _transparencyPercent;
    private string _currentContent = "";
    private string? _backgroundColorHex;
    private List<string> _editModeAttachmentTokens = new();

    private const string AttachmentPlaceholder = "[— вложение —]";
    private static readonly Regex AttachmentBlockRegex = new(
        @"\[Файл:\s*[^\]]*\]|\[Изображение:\s*[^\]]*\]|\[Папка:\s*[^\]]*\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public Action<string>? OnContentSaved { get; set; }

    public StickerWindow()
    {
        InitializeComponent();
        _transparencyPercent = 100;
        ApplyCachedIcon();
        Closed += (_, _) =>
        {
            _countdownTimer?.Stop();
            _countdownTimer = null;
        };
    }

    private void ApplyCachedIcon()
    {
        if (AppIconCache.Icon != null)
            Icon = AppIconCache.Icon;
    }

    public StickerWindow(string title, string content, int? durationMinutes = null, string? backgroundColorHex = null,
        int transparencyPercent = 100, bool isClickThrough = false, bool isPinned = true, bool closeOnTimerEnd = false, DateTime? deadline = null) : this()
    {
        _closeOnTimerEnd = closeOnTimerEnd;
        _transparencyPercent = Math.Clamp(transparencyPercent, 1, 100);
        _currentContent = content ?? "";
        _backgroundColorHex = backgroundColorHex;
        Title = title;
        Topmost = isPinned;
        Opacity = 1;
        BuildContentWithLinks(_currentContent);
        ApplyBackgroundColor(backgroundColorHex);
        if (deadline.HasValue)
        {
            DeadlineText.Text = "До " + deadline.Value.ToString("dd.MM.yyyy HH:mm");
            DeadlineText.IsVisible = true;
        }
        if (isClickThrough)
            Opened += OnOpenedSetClickThrough;
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
        TrySetClickThrough(true);
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

    private const int StickerImageMaxWidth = 240;
    private const int StickerImageMaxHeight = 200;

    private void BuildContentWithLinks(string? content)
    {
        ContentPanel.Children.Clear();
        if (string.IsNullOrEmpty(content))
            return;

        var normalBrush = (Avalonia.Application.Current?.Resources?.TryGetResource("StickerForeground", null, out var n) == true && n is IBrush nb) ? nb : Brushes.Black;
        var linkBrush = (Avalonia.Application.Current?.Resources?.TryGetResource("AccentBrush", null, out var a) == true && a is IBrush ab) ? ab : Brushes.Blue;

        var blocks = ContentParser.Parse(content);
        foreach (var block in blocks)
        {
            switch (block)
            {
                case TextContentBlock textBlock:
                    if (IsSingleLineImagePath(textBlock.Text))
                        AddImageOrFallback(ContentPanel, textBlock.Text.Trim(), normalBrush);
                    else
                        AddTextWithLinks(ContentPanel, textBlock.Text, normalBrush, linkBrush);
                    break;
                case FileContentBlock fileBlock:
                    AddFileLink(ContentPanel, fileBlock.Path, normalBrush, linkBrush);
                    break;
                case ImageContentBlock imageBlock:
                    AddImageOrFallback(ContentPanel, imageBlock.Path, normalBrush);
                    break;
                default:
                    break;
            }
        }
    }

    private static bool IsSingleLineImagePath(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim();
        if (t.Contains('\n') || t.Contains('\r')) return false;
        if (!File.Exists(t)) return false;
        var ext = System.IO.Path.GetExtension(t);
        return ext.Length > 1 && s_imageExtensions.Contains(ext.AsSpan(1).ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private static readonly string[] s_imageExtensions = { "png", "jpg", "jpeg", "gif", "bmp", "webp" };

    private static void AddTextWithLinks(Panel parent, string text, IBrush normalBrush, IBrush linkBrush)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var linePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
            var lastIndex = 0;
            foreach (Match m in UrlRegex.Matches(line))
            {
                if (m.Index > lastIndex)
                {
                    linePanel.Children.Add(new TextBlock
                    {
                        Text = line[lastIndex..m.Index],
                        FontSize = 14,
                        Foreground = normalBrush,
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                var url = m.Value.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "https://" + m.Value : m.Value;
                var linkBlock = new TextBlock
                {
                    Text = m.Value,
                    FontSize = 14,
                    Foreground = linkBrush,
                    TextDecorations = TextDecorations.Underline,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    TextWrapping = TextWrapping.Wrap
                };
                linkBlock.PointerPressed += (_, e) =>
                {
                    e.Handled = true;
                    try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
                };
                linePanel.Children.Add(linkBlock);
                lastIndex = m.Index + m.Length;
            }
            if (lastIndex < line.Length)
                linePanel.Children.Add(new TextBlock
                {
                    Text = line[lastIndex..],
                    FontSize = 14,
                    Foreground = normalBrush,
                    TextWrapping = TextWrapping.Wrap
                });
            parent.Children.Add(linePanel);
        }
    }

    private static void AddFileLink(Panel parent, string path, IBrush normalBrush, IBrush linkBrush)
    {
        var name = System.IO.Path.GetFileName(path.Trim());
        var block = new TextBlock
        {
            Text = string.IsNullOrEmpty(name) ? path : name,
            FontSize = 14,
            Foreground = linkBrush,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
            TextWrapping = TextWrapping.Wrap
        };
        block.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path.Trim()))
            {
                try { Process.Start(new ProcessStartInfo { FileName = path.Trim(), UseShellExecute = true }); } catch { }
            }
        };
        parent.Children.Add(block);
    }

    private void AddImageOrFallback(Panel parent, string path, IBrush normalBrush)
    {
        var trimmed = path.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        if (!File.Exists(trimmed))
        {
            parent.Children.Add(new TextBlock { Text = trimmed, FontSize = 14, Foreground = normalBrush, TextWrapping = TextWrapping.Wrap });
            return;
        }

        try
        {
            var bitmap = new Bitmap(trimmed);
            var image = new Avalonia.Controls.Image
            {
                Source = bitmap,
                MaxWidth = StickerImageMaxWidth,
                MaxHeight = StickerImageMaxHeight,
                Stretch = Stretch.Uniform
            };
            parent.Children.Add(image);
        }
        catch
        {
            parent.Children.Add(new TextBlock { Text = trimmed, FontSize = 14, Foreground = normalBrush, TextWrapping = TextWrapping.Wrap });
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.H && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            e.Handled = true;
            Hide();
        }
    }

    private void OnEditModeClick(object? sender, RoutedEventArgs e)
    {
        Opacity = 1;
        ApplyBackgroundColorSolid(_backgroundColorHex);
        ViewScroll.IsVisible = false;
        ContentPanel.IsVisible = false;
        EditPanel.IsVisible = true;
        var (textForEditing, tokens) = GetContentForEditing(_currentContent);
        _editModeAttachmentTokens = tokens;
        EditContentBox.Text = textForEditing;
        EditContentBox.Focus();
        EditModeButton.IsVisible = false;
        EditButtonsPanel.IsVisible = true;
    }

    private static (string textForEditing, List<string> attachmentTokens) GetContentForEditing(string? content)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(content))
            return ("", tokens);
        var text = AttachmentBlockRegex.Replace(content, m =>
        {
            tokens.Add(m.Value);
            return AttachmentPlaceholder;
        });
        return (text, tokens);
    }

    private static string RestoreContentWithAttachments(string editedText, List<string> attachmentTokens)
    {
        var result = editedText ?? "";
        foreach (var token in attachmentTokens)
        {
            var idx = result.IndexOf(AttachmentPlaceholder, StringComparison.Ordinal);
            if (idx < 0) break;
            result = result.Substring(0, idx) + token + result.Substring(idx + AttachmentPlaceholder.Length);
        }
        return result;
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
        var editedText = EditContentBox.Text ?? "";
        _currentContent = RestoreContentWithAttachments(editedText, _editModeAttachmentTokens);
        _editModeAttachmentTokens.Clear();
        OnContentSaved?.Invoke(_currentContent);
        ShowViewMode();
    }

    private void OnCancelEditClick(object? sender, RoutedEventArgs e)
    {
        _editModeAttachmentTokens.Clear();
        ShowViewMode();
    }

    private void ShowViewMode()
    {
        EditPanel.IsVisible = false;
        ViewScroll.IsVisible = true;
        ContentPanel.IsVisible = true;
        BuildContentWithLinks(_currentContent);
        EditButtonsPanel.IsVisible = false;
        EditModeButton.IsVisible = true;
        ApplyBackgroundColor(_backgroundColorHex);
    }
}
