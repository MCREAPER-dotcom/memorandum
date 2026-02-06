using Avalonia.Controls;
using Avalonia.Interactivity;
using Memorandum.Desktop;

namespace Memorandum.Desktop.Views;

public partial class DeadlineReminderWindow : MemorandumDialogWindow
{
    public DeadlineReminderWindow()
    {
        InitializeComponent();
    }

    public void SetNote(string noteTitle, string deadlineText)
    {
        NoteTitleText.Text = "«" + noteTitle + "»";
        DeadlineTimeText.Text = "Дата и время: " + deadlineText;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
