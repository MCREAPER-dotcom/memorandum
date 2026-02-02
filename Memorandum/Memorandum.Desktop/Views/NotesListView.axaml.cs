using Avalonia.Controls;

namespace Memorandum.Desktop.Views;

public partial class NotesListView : UserControl
{
    public NotesListView()
    {
        InitializeComponent();
        SortCombo.ItemsSource = new[] { "По дате изменения", "По дате создания", "По названию" };
        SortCombo.SelectedIndex = 0;
    }

    public void ScrollToTop()
    {
        NotesScrollViewer.ScrollToHome();
    }
}
