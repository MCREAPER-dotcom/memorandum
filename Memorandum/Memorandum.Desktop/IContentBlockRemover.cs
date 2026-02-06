using Memorandum.Desktop.Models;

namespace Memorandum.Desktop;

/// <summary>
/// Контрол, который может удалять блоки вложений (файл/изображение) из контента. Реализуется формой редактирования заметки и панелью редактирования стикера.
/// </summary>
public interface IContentBlockRemover
{
    void RemoveBlock(ContentBlockItem block);
}
