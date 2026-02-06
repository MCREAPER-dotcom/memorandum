using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Memorandum.Desktop;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Controls;

/// <summary>
/// Переиспользуемый компонент для отображения вложенного файла.
/// Показывает иконку документа, название файла с расширением и кнопку открытия.
/// </summary>
public partial class FileAttachmentView : UserControl
{
    private string? _filePath;

    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<FileAttachmentView, string?>(nameof(FilePath));

    public static readonly StyledProperty<string?> FileDisplayNameProperty =
        AvaloniaProperty.Register<FileAttachmentView, string?>(nameof(FileDisplayName));

    public string? FilePath
    {
        get => GetValue(FilePathProperty);
        set
        {
            SetValue(FilePathProperty, value);
            UpdateDisplay();
        }
    }

    /// <summary>Отображаемое имя файла (оригинальное). Если пусто — берётся из пути.</summary>
    public string? FileDisplayName
    {
        get => GetValue(FileDisplayNameProperty);
        set
        {
            SetValue(FileDisplayNameProperty, value);
            UpdateDisplay();
        }
    }

    public FileAttachmentView()
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
        if (DataContext is not FileContentBlock block) return;
        FindContentBlockRemover()?.RemoveBlock(block);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FilePathProperty || change.Property == FileDisplayNameProperty)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            FileNameText.Text = "";
            FileExtensionText.Text = "";
            return;
        }

        var path = FilePath.Trim();
        var fromPath = Path.GetFileName(path);
        var pathExtension = Path.GetExtension(path);
        var pathHasExtension = !string.IsNullOrEmpty(pathExtension);
        
        string display;
        if (!string.IsNullOrWhiteSpace(FileDisplayName))
        {
            display = FileDisplayName.Trim();
            if (pathHasExtension && !display.Contains('.'))
            {
                display = fromPath;
            }
            else if (pathHasExtension && !display.EndsWith(pathExtension, StringComparison.OrdinalIgnoreCase))
            {
                display = Path.GetFileNameWithoutExtension(display) + pathExtension;
            }
        }
        else
        {
            display = fromPath;
        }
        
        FileNameText.Text = display;
        FileExtensionText.Text = "";

        if (DocumentImage != null && DocumentImage.Source == null)
        {
            if (Application.Current?.Resources.TryGetResource("FileIcon", null, out var fileIcon) == true && fileIcon != null)
                DocumentImage.Source = fileIcon as Avalonia.Media.IImage;
        }
    }

    private void OnOpenFileClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath.Trim()))
            return;

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = FilePath.Trim(),
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
        catch
        {
            // файл не открыт
        }
    }
}
