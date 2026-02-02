using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Views;

public partial class TagNameDialog : Window
{
    public TagNameDialog()
    {
        InitializeComponent();
        SelectedColorKey = PaletteConstants.TagPillResourceKeys.Length > 0 ? PaletteConstants.TagPillResourceKeys[0] : "";
        BuildColorPanel();
    }

    public string? Result { get; private set; }
    public TagCreationResult? CreationResult { get; private set; }
    private string SelectedColorKey { get; set; }

    private void BuildColorPanel()
    {
        ColorPanel.Children.Clear();
        foreach (var key in PaletteConstants.TagPillResourceKeys)
        {
            IBrush brush = Avalonia.Application.Current?.Resources?.TryGetResource(key, null, out var value) == true && value is IBrush b
                ? b
                : new SolidColorBrush(Color.Parse(PaletteConstants.DefaultTagPillFallbackHex));
            var border = new Border
            {
                Tag = key,
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = brush,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
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
        Close(CreationResult);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
