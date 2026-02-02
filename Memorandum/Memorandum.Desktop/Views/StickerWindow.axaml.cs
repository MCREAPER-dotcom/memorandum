using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
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

    public Action<string>? OnContentSaved { get; set; }

    public StickerWindow()
    {
        InitializeComponent();
        _transparencyPercent = 100;
        TrySetAppIcon();
        Closed += (_, _) =>
        {
            _countdownTimer?.Stop();
            _countdownTimer = null;
        };
    }

    private void TrySetAppIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Memorandum-AppIcon.png");
            if (File.Exists(path))
                Icon = new WindowIcon(path);
        }
        catch
        {
            // иконка не задана
        }
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

    private void BuildContentWithLinks(string? content)
    {
        ContentPanel.Children.Clear();
        if (string.IsNullOrEmpty(content))
            return;

        var lines = content.Split('\n');
        var normalBrush = (Avalonia.Application.Current?.Resources?.TryGetResource("StickerForeground", null, out var n) == true && n is IBrush nb) ? nb : Brushes.Black;
        var linkBrush = (Avalonia.Application.Current?.Resources?.TryGetResource("AccentBrush", null, out var a) == true && a is IBrush ab) ? ab : Brushes.Blue;

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
            ContentPanel.Children.Add(linePanel);
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
        EditContentBox.Text = _currentContent;
        EditContentBox.Focus();
        EditModeButton.IsVisible = false;
        EditButtonsPanel.IsVisible = true;
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
        _currentContent = EditContentBox.Text ?? "";
        OnContentSaved?.Invoke(_currentContent);
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
        ContentPanel.IsVisible = true;
        BuildContentWithLinks(_currentContent);
        EditButtonsPanel.IsVisible = false;
        EditModeButton.IsVisible = true;
        ApplyBackgroundColor(_backgroundColorHex);
    }
}
