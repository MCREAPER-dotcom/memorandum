using System.Threading.Tasks;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Модульный сервис создания папок и тегов: диалог → добавление в модель → обновление UI.
/// Подключается в представлениях (редактор заметки, форма пресета) вместо набора отдельных callbacks.
/// </summary>
public interface IFolderTagCreationService
{
    /// <summary>Показать диалог «Добавить папку», создать корневую папку, обновить сайдбар. Возвращает имя или null.</summary>
    Task<string?> AddRootFolderAsync();

    /// <summary>Показать диалог «Вложенная папка», создать подпапку, обновить сайдбар. Возвращает имя или null.</summary>
    Task<string?> AddSubfolderAsync(string parentPath);

    /// <summary>Показать диалог создания тега, добавить тег в приложение, обновить сайдбар. Возвращает результат или null.</summary>
    Task<TagCreationResult?> CreateTagAsync(string dialogTitle);
}
