using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Memorandum.Desktop.Views;

public partial class DeadlineReminderWindow : Window
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
