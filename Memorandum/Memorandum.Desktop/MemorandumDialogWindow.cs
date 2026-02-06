using Avalonia.Controls;

namespace Memorandum.Desktop;

/// <summary>
/// Базовый класс для диалогов приложения: общая рамка (без системного оформления), иконка и заголовок задаются через DialogChrome.
/// В конструкторе выставляются SystemDecorations="None", ExtendClientAreaToDecorationsHint="True", фон.
/// </summary>
public class MemorandumDialogWindow : Window
{
    public MemorandumDialogWindow()
    {
        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        if (Avalonia.Application.Current?.Resources?.TryGetResource("AppBackground", null, out var bg) == true && bg is Avalonia.Media.IBrush brush)
            Background = brush;
    }
}
