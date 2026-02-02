using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Memorandum.Desktop.ViewModels;

public sealed partial class TagItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public string Name { get; }
    public int Count { get; }
    public ICommand SelectCommand { get; }

    public TagItemViewModel(string name, int count, bool isSelected, ICommand selectCommand)
    {
        Name = name;
        Count = count;
        _isSelected = isSelected;
        SelectCommand = selectCommand;
    }
}
