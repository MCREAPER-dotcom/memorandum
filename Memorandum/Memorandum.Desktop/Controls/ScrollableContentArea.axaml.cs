using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Memorandum.Desktop.Controls;

/// <summary>
/// Область с прокруткой для переиспользования в списке заметок, форме редактирования, настройках и т.д.
/// </summary>
public class ScrollableContentArea : ContentControl
{
    private ScrollViewer? _scrollViewer;

    public ScrollableContentArea()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Padding = new Thickness(24, 24, 24, 0);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
    }

    public void ScrollToHome()
    {
        _scrollViewer?.ScrollToHome();
    }
}
