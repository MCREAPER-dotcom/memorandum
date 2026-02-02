namespace Memorandum.Desktop.Models;

/// <summary>
/// Один пункт настройки горячей клавиши: идентификатор действия, отображаемое имя и текущее сочетание.
/// Хранится отдельно от логики выполнения действия.
/// </summary>
public sealed class HotkeyConfigItem
{
    public string ActionId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string KeyCombo { get; set; } = "";
}
