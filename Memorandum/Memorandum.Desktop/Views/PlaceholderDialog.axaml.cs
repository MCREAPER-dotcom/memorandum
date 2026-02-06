using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Memorandum.Desktop;

namespace Memorandum.Desktop.Views;

public partial class PlaceholderDialog : MemorandumDialogWindow
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

    /// <summary>Показать диалог с заданными заголовком и текстом (переиспользуемый экземпляр).</summary>
    public Task ShowReusableAsync(Window owner, string title, string message)
    {
        Title = title;
        Message = message;
        var tcs = new TaskCompletionSource<bool>();
        void OnClosed(object? _, EventArgs __)
        {
            Closed -= OnClosed;
            tcs.TrySetResult(true);
        }
        Closed += OnClosed;
        Dispatcher.UIThread.Post(() => ShowDialog(owner));
        return tcs.Task;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
