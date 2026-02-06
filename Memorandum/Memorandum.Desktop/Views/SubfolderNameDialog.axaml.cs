using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Memorandum.Desktop;
using Memorandum.Desktop.Services;
using IModalOverlayHost = Memorandum.Desktop.IModalOverlayHost;

namespace Memorandum.Desktop.Views;

public partial class SubfolderNameDialog : MemorandumDialogWindow
{
    private TaskCompletionSource<string?>? _tcs;
    private Window? _modalOwner;

    public SubfolderNameDialog()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            _tcs?.TrySetResult(null);
            if (_modalOwner != null)
            {
                if (_modalOwner is IModalOverlayHost host)
                    host.SetModalOverlayVisible(false);
                else
                    _modalOwner.IsEnabled = true;
                _modalOwner = null;
            }
        };
    }

    public string? Result { get; private set; }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var name = (NameBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return;
        Result = name;
        CompleteWithResult(Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => CompleteWithResult(null);

    private void CompleteWithResult(string? result)
    {
        if (_tcs == null) return;
        _tcs.TrySetResult(result);
        _tcs = null;
        if (_modalOwner != null)
        {
            if (_modalOwner is IModalOverlayHost host)
                host.SetModalOverlayVisible(false);
            else
                _modalOwner.IsEnabled = true;
            _modalOwner = null;
        }
        Hide();
    }

    public void ResetForShow(string title)
    {
        Title = title;
        NameBox.Text = "";
    }

    public async Task<string?> ShowModalReusableAsync(Window owner, string title)
    {
        ResetForShow(title);
        _tcs = new TaskCompletionSource<string?>();
        _modalOwner = owner;
        if (owner is IModalOverlayHost host)
            host.SetModalOverlayVisible(true);
        else
            owner.IsEnabled = false;
        Show(owner);
        try
        {
            return await _tcs.Task.ConfigureAwait(true);
        }
        finally
        {
            if (_modalOwner != null)
            {
                if (_modalOwner is IModalOverlayHost h)
                    h.SetModalOverlayVisible(false);
                else
                    _modalOwner.IsEnabled = true;
                _modalOwner = null;
            }
        }
    }
}
