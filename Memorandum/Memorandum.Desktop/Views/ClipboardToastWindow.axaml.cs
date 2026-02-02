using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.Views;

public partial class ClipboardToastWindow : Window
{
    public ClipboardToastWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        PositionToBottomRight();
        Avalonia.Threading.DispatcherTimer.RunOnce(Close, TimeSpan.FromSeconds(2.5));
    }

    private void PositionToBottomRight()
    {
        var (x, y, w, h) = ScreenshotClipboardService.GetVirtualScreenBounds();
        var toastW = 320;
        Position = new PixelPoint(x + w - toastW - 24, y + h - 100);
    }
}
