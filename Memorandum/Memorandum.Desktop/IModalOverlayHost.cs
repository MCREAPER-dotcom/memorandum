namespace Memorandum.Desktop;

/// <summary>
/// Окно, которое может показывать полупрозрачный оверлей при открытии модального диалога (вместо отключения всего окна).
/// </summary>
public interface IModalOverlayHost
{
    void SetModalOverlayVisible(bool visible);
}
