using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Memorandum.Desktop;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Controls;

/// <summary>
/// Переиспользуемый компонент для отображения вложенного изображения.
/// Показывает превью и кнопку открытия (search) в правом верхнем углу.
/// </summary>
public partial class ImageAttachmentView : UserControl
{
    public static readonly StyledProperty<string?> ImagePathProperty =
        AvaloniaProperty.Register<ImageAttachmentView, string?>(nameof(ImagePath));

    public string? ImagePath
    {
        get => GetValue(ImagePathProperty);
        set
        {
            SetValue(ImagePathProperty, value);
            UpdatePreview();
        }
    }

    public ImageAttachmentView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (FindContentBlockRemover() != null && DeleteButton != null)
            DeleteButton.IsVisible = true;
    }

    private IContentBlockRemover? FindContentBlockRemover()
    {
        foreach (var ancestor in this.GetVisualAncestors())
        {
            if (ancestor is IContentBlockRemover r)
                return r;
        }
        return null;
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ImageContentBlock block) return;
        FindContentBlockRemover()?.RemoveBlock(block);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ImagePathProperty)
            UpdatePreview();
    }

    private void UpdatePreview()
    {
        var path = ImagePath?.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            PreviewImage.Source = null;
            return;
        }

        try
        {
            PreviewImage.Source = new Bitmap(path);
        }
        catch
        {
            PreviewImage.Source = null;
        }
    }

    private void OnOpenImageClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath.Trim()))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ImagePath.Trim(),
                UseShellExecute = true
            });
        }
        catch
        {
            // изображение не открыто
        }
    }
}
