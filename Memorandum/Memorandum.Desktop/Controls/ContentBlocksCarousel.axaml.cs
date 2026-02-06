using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Controls;

/// <summary>
/// Слайдер вложений: по одному файлу/изображению с точками-индикаторами и переключением.
/// </summary>
public partial class ContentBlocksCarousel : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ContentBlocksCarousel, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ContentBlocksCarousel, IDataTemplate?>(nameof(ItemTemplate));

    private IDataTemplate? _template;
    private List<ContentBlockItem>? _items;
    private int _selectedIndex;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public ContentBlocksCarousel()
    {
        if (Avalonia.Application.Current?.Resources.TryGetResource("ContentBlockTemplateSelector", null, out var res) == true && res is IDataTemplate t)
            _template = t;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty || change.Property == ItemTemplateProperty)
            RefreshFromSource();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RefreshFromSource();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(RefreshFromSource, DispatcherPriority.Loaded);
    }

    private void RefreshFromSource()
    {
        _items = null;
        var src = ItemsSource;
        if (src != null)
        {
            _items = src.OfType<ContentBlockItem>().ToList();
            if (_items.Count == 0)
                _items = null;
        }

        if (ItemTemplate != null)
            _template = ItemTemplate;
        else if (Avalonia.Application.Current?.Resources.TryGetResource("ContentBlockTemplateSelector", null, out var res) == true && res is IDataTemplate t)
            _template = t;
        _selectedIndex = 0;
        UpdateContent();
        BuildDots();
    }

    private void UpdateContent()
    {
        if (ContentHost == null) return;
        ContentHost.Child = null;
        if (_items == null || _items.Count == 0) return;
        var idx = _selectedIndex;
        if (idx < 0 || idx >= _items.Count)
            idx = 0;
        _selectedIndex = idx;
        var item = _items[idx];
        var control = _template?.Build(item);
        if (control != null)
        {
            control.DataContext = item;
            if (control is FileAttachmentView fv && item is FileContentBlock fb)
            {
                fv.FilePath = fb.Path;
                fv.FileDisplayName = fb.DisplayName;
            }
            else if (control is ImageAttachmentView iv && item is ImageContentBlock ib)
                iv.ImagePath = ib.Path;
            ContentHost.Child = control;
        }
    }

    private void BuildDots()
    {
        if (DotsPanel == null) return;
        DotsPanel.Children.Clear();
        var count = _items?.Count ?? 0;
        DotsPanel.IsVisible = count > 1;
        if (count <= 1) return;
        for (var i = 0; i < count; i++)
        {
            var index = i;
            var dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = i == _selectedIndex
                    ? (Avalonia.Application.Current?.Resources.TryGetResource("AccentBrush", null, out var ac) == true && ac is IBrush accent ? accent : Brushes.Purple)
                    : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            dot.PointerPressed += (_, _) => GoTo(index);
            DotsPanel.Children.Add(dot);
        }
    }

    private void GoTo(int index)
    {
        if (_items == null || index < 0 || index >= _items.Count) return;
        _selectedIndex = index;
        UpdateContent();
        UpdateDotsSelection();
    }

    private void UpdateDotsSelection()
    {
        if (DotsPanel?.Children == null || _items == null) return;
        for (var i = 0; i < DotsPanel.Children.Count && i < _items.Count; i++)
        {
            if (DotsPanel.Children[i] is Border dot)
                dot.Background = i == _selectedIndex
                    ? (Avalonia.Application.Current?.Resources.TryGetResource("AccentBrush", null, out var ab) == true && ab is IBrush acc ? acc : Brushes.Purple)
                    : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
        }
    }
}
