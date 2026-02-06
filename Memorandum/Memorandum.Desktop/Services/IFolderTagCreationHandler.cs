using System.Threading.Tasks;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Контракт для хоста (например MainWindow): показ диалогов и применение изменений при создании папки/тега.
/// Реализацию создаёт приложение; сервис <see cref="IFolderTagCreationService"/> вызывает методы при действиях пользователя.
/// </summary>
public interface IFolderTagCreationHandler
{
    Task<string?> ShowFolderDialogAsync(string title);
    Task<TagCreationResult?> ShowTagDialogAsync(string title);
    void AddRootFolder(string name);
    void AddSubfolder(string parentPath, string name);
    void AddKnownTag(string name, string? colorKey);
    void Refresh();
}
