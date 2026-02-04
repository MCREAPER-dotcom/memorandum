using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;
using Memorandum.Desktop.Themes;
using IModalOverlayHost = Memorandum.Desktop.IModalOverlayHost;

namespace Memorandum.Desktop.Views;

public partial class TagNameDialog : Window
{
    private static IBrush[]? _cachedBrushes;
    private TaskCompletionSource<TagCreationResult?>? _tcs;
    private Window? _modalOwner;

    public TagNameDialog()
    {
        InitializeComponent();
        SelectedColorKey = PaletteConstants.TagPillResourceKeys.Length > 0 ? PaletteConstants.TagPillResourceKeys[0] : "";
        BuildColorPanel();
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

    /// <summary>Вызвать при старте приложения, чтобы палитра тегов была готова до первого открытия диалога.</summary>
    public static void Preload()
    {
        EnsureBrushCache();
    }

    private static void EnsureBrushCache()
    {
        if (_cachedBrushes != null)
            return;
        var keys = PaletteConstants.TagPillResourceKeys;
        var list = new IBrush[keys.Length];
        var fallback = new SolidColorBrush(Color.Parse(PaletteConstants.DefaultTagPillFallbackHex));
        for (var i = 0; i < keys.Length; i++)
        {
            if (Avalonia.Application.Current?.Resources?.TryGetResource(keys[i], null, out var value) == true && value is IBrush b)
                list[i] = b;
            else
                list[i] = fallback;
        }
        _cachedBrushes = list;
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
    public TagCreationResult? CreationResult { get; private set; }
    private string SelectedColorKey { get; set; }

    private void BuildColorPanel()
    {
        EnsureBrushCache();
        ColorPanel.Children.Clear();
        var keys = PaletteConstants.TagPillResourceKeys;
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var border = new Border
            {
                Tag = key,
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = _cachedBrushes![i],
                BorderThickness = new Thickness(0),
                Margin = new Thickness(6),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = null
            };
            if (key == SelectedColorKey)
            {
                border.BorderThickness = new Thickness(3);
                border.BorderBrush = Brushes.White;
            }
            border.PointerPressed += OnColorPressed;
            ColorPanel.Children.Add(border);
        }
    }

    private void OnColorPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string key)
            return;
        SelectedColorKey = key;
        foreach (var child in ColorPanel.Children)
        {
            if (child is Border b)
            {
                var selected = b.Tag as string == key;
                b.BorderThickness = selected ? new Thickness(3) : new Thickness(0);
                b.BorderBrush = selected ? Brushes.White : null;
            }
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var name = (TagNameBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return;
        Result = name;
        CreationResult = new TagCreationResult(name, SelectedColorKey);
        CompleteWithResult(CreationResult);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => CompleteWithResult(null);

    private void CompleteWithResult(TagCreationResult? result)
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
        TagNameBox.Text = "";
        var keys = PaletteConstants.TagPillResourceKeys;
        SelectedColorKey = keys.Length > 0 ? keys[0] : "";
        foreach (var child in ColorPanel.Children)
        {
            if (child is Border b && b.Tag is string key)
            {
                b.BorderThickness = key == SelectedColorKey ? new Thickness(3) : new Thickness(0);
                b.BorderBrush = key == SelectedColorKey ? Brushes.White : null;
            }
        }
    }

    public async Task<TagCreationResult?> ShowModalReusableAsync(Window owner, string title)
    {
        ResetForShow(title);
        _tcs = new TaskCompletionSource<TagCreationResult?>();
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
