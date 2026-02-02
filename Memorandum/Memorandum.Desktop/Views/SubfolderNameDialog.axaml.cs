using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Memorandum.Desktop.Views;

public partial class SubfolderNameDialog : Window
{
    public SubfolderNameDialog()
    {
        InitializeComponent();
    }

    public string? Result { get; private set; }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var name = (NameBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return;
        Result = name;
        Close(Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
