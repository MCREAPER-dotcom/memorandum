using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Memorandum.Desktop.Views;

public partial class PlaceholderDialog : Window
{
    public PlaceholderDialog()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => MessageText.Text ?? "";
        set => MessageText.Text = value;
    }

    private async void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
        await System.Threading.Tasks.Task.CompletedTask;
    }
}
