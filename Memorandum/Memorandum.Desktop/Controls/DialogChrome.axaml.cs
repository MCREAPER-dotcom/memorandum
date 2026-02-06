using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.Controls;

/// <summary>
/// Переиспользуемая рамка диалога: заголовок с иконкой приложения, названием и кнопками — / □ ×.
/// Контент — тело диалога (один дочерний элемент).
/// </summary>
public partial class DialogChrome : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DialogChrome, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private Image? _titleBarIcon;
    private TextBlock? _titleBarTitle;

    public DialogChrome()
    {
        Title = "";
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _titleBarIcon = e.NameScope.Find<Image>("TitleBarIcon");
        _titleBarTitle = e.NameScope.Find<TextBlock>("TitleBarTitle");
        ApplyCachedIcon();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty && _titleBarTitle != null)
            _titleBarTitle.Text = Title;
    }

    private void ApplyCachedIcon()
    {
        if (AppIconCache.Icon == null)
            return;
        var w = this.FindAncestorOfType<Window>();
        if (w != null)
            w.Icon = AppIconCache.Icon;
        if (_titleBarIcon != null && AppIconCache.Bitmap != null)
        {
            _titleBarIcon.Source = AppIconCache.Bitmap;
            _titleBarIcon.IsVisible = true;
        }
    }

    private Window? GetWindow() => this.FindAncestorOfType<Window>();

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
        GetWindow()?.BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        var w = GetWindow();
        if (w != null)
            w.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        var w = GetWindow();
        if (w == null) return;
        w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => GetWindow()?.Close();

    /// <summary>Обновить отображаемый заголовок (при изменении Title из кода).</summary>
    public void RefreshTitle()
    {
        if (_titleBarTitle != null)
            _titleBarTitle.Text = Title;
    }
}
