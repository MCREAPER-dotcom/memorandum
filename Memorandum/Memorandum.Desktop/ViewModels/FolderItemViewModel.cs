using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Memorandum.Desktop.ViewModels;

public sealed partial class FolderItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public string Path { get; }
    public string DisplayName { get; }
    public int Count { get; }
    public int Depth { get; }
    public ICommand SelectCommand { get; }
    public ICommand AddSubfolderCommand { get; }

    public FolderItemViewModel(string path, string displayName, int count, int depth, bool isSelected,
        ICommand selectCommand, ICommand addSubfolderCommand)
    {
        Path = path;
        DisplayName = displayName;
        Count = count;
        Depth = depth;
        _isSelected = isSelected;
        SelectCommand = selectCommand;
        AddSubfolderCommand = addSubfolderCommand;
    }
}
