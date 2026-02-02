using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.Views;

public partial class ScreenshotOverlayWindow : Window
{
    private Point _startPoint;
    private Point _currentPoint;
    private bool _isSelecting;

    public ScreenshotOverlayWindow()
    {
        InitializeComponent();
        RootPanel.PointerPressed += OnPointerPressed;
        RootPanel.PointerMoved += OnPointerMoved;
        RootPanel.PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(RootPanel).Properties.IsLeftButtonPressed)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(RootPanel);
            _currentPoint = _startPoint;
            DimTop.IsVisible = DimBottom.IsVisible = DimLeft.IsVisible = DimRight.IsVisible = true;
            UpdateSelectionBorder();
            SelectionBorder.IsVisible = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting) return;
        _currentPoint = e.GetPosition(RootPanel);
        UpdateSelectionBorder();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSelecting || e.GetCurrentPoint(RootPanel).Properties.PointerUpdateKind != Avalonia.Input.PointerUpdateKind.LeftButtonReleased)
            return;

        _isSelecting = false;
        var x = (int)Math.Min(_startPoint.X, _currentPoint.X);
        var y = (int)Math.Min(_startPoint.Y, _currentPoint.Y);
        var w = (int)Math.Abs(_currentPoint.X - _startPoint.X);
        var h = (int)Math.Abs(_currentPoint.Y - _startPoint.Y);

        if (w >= 4 && h >= 4)
        {
            var (vx, vy, _, _) = ScreenshotClipboardService.GetVirtualScreenBounds();
            int screenX = vx + x;
            int screenY = vy + y;
            var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (ScreenshotClipboardService.CaptureRegionToClipboardWindows(hwnd, screenX, screenY, w, h))
            {
                ShowToastAndClose();
                return;
            }
        }

        Close();
    }

    private void UpdateSelectionBorder()
    {
        var x = Math.Min(_startPoint.X, _currentPoint.X);
        var y = Math.Min(_startPoint.Y, _currentPoint.Y);
        var w = Math.Abs(_currentPoint.X - _startPoint.X);
        var h = Math.Abs(_currentPoint.Y - _startPoint.Y);
        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;

        var totalW = RootPanel.Bounds.Width > 0 ? RootPanel.Bounds.Width : Width;
        var totalH = RootPanel.Bounds.Height > 0 ? RootPanel.Bounds.Height : Height;
        if (totalW <= 0 || totalH <= 0) return;

        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);
        DimTop.Width = totalW;
        DimTop.Height = Math.Max(0, y);

        Canvas.SetLeft(DimBottom, 0);
        Canvas.SetTop(DimBottom, y + h);
        DimBottom.Width = totalW;
        DimBottom.Height = Math.Max(0, totalH - (y + h));

        Canvas.SetLeft(DimLeft, 0);
        Canvas.SetTop(DimLeft, y);
        DimLeft.Width = Math.Max(0, x);
        DimLeft.Height = Math.Max(0, h);

        Canvas.SetLeft(DimRight, x + w);
        Canvas.SetTop(DimRight, y);
        DimRight.Width = Math.Max(0, totalW - (x + w));
        DimRight.Height = Math.Max(0, h);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void ShowToastAndClose()
    {
        Close();
        Dispatcher.UIThread.Post(() =>
        {
            var toast = new ClipboardToastWindow();
            toast.Show();
        });
    }
}
