using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Memorandum.Desktop.Services;
using IModalOverlayHost = Memorandum.Desktop.IModalOverlayHost;

namespace Memorandum.Desktop.Views;

public partial class SubfolderNameDialog : Window
{
    private TaskCompletionSource<string?>? _tcs;
    private Window? _modalOwner;

    public SubfolderNameDialog()
    {
        InitializeComponent();
        ApplyCachedIcon();
        Closed += (_, _) =>
        {
            _tcs?.TrySetResult(null);
            if (_modalOwner != null)
            {
                if (_modalOwner is IModalOverlayHost host)
                    host.SetModalOverlayVisible(false);
                else
                    _modalOwner.IsEnabled = true;
                _modalOwner = null;
            }
        };
    }

    private void ApplyCachedIcon()
    {
        if (AppIconCache.Icon == null)
            return;
        Icon = AppIconCache.Icon;
        if (TitleBarIcon != null && AppIconCache.Bitmap != null)
        {
            TitleBarIcon.Source = AppIconCache.Bitmap;
            TitleBarIcon.IsVisible = true;
        }
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

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e) => CompleteWithResult(null);

    public string? Result { get; private set; }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var name = (NameBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return;
        Result = name;
        CompleteWithResult(Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => CompleteWithResult(null);

    private void CompleteWithResult(string? result)
    {
        if (_tcs == null) return;
        _tcs.TrySetResult(result);
        _tcs = null;
        if (_modalOwner != null)
        {
            if (_modalOwner is IModalOverlayHost host)
                host.SetModalOverlayVisible(false);
            else
                _modalOwner.IsEnabled = true;
            _modalOwner = null;
        }
        Hide();
    }

    public void ResetForShow(string title)
    {
        Title = title;
        if (TitleBarTitle != null)
            TitleBarTitle.Text = title;
        NameBox.Text = "";
    }

    public async Task<string?> ShowModalReusableAsync(Window owner, string title)
    {
        ResetForShow(title);
        _tcs = new TaskCompletionSource<string?>();
        _modalOwner = owner;
        if (owner is IModalOverlayHost host)
            host.SetModalOverlayVisible(true);
        else
            owner.IsEnabled = false;
        Show(owner);
        try
        {
            return await _tcs.Task.ConfigureAwait(true);
        }
        finally
        {
            if (_modalOwner != null)
            {
                if (_modalOwner is IModalOverlayHost h)
                    h.SetModalOverlayVisible(false);
                else
                    _modalOwner.IsEnabled = true;
                _modalOwner = null;
            }
        }
    }
}
