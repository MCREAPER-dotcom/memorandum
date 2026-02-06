using Memorandum.Desktop.Views;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Централизованная предзагрузка ресурсов для всех диалогов (иконка, палитра тегов, шаблоны).
/// Вызывать при старте приложения до первого открытия любого окна с DialogChrome.
/// </summary>
public static class DialogPreloader
{
    /// <summary>Предзагрузить иконку приложения, палитру тегов и прочие ресурсы для диалогов.</summary>
    public static void Preload()
    {
        AppIconCache.Preload();
        TagNameDialog.Preload();
    }
}
