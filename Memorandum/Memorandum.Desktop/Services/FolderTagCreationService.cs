using System.Threading.Tasks;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Реализация <see cref="IFolderTagCreationService"/>: делегирует диалоги и применение изменений обработчику.
/// </summary>
public sealed class FolderTagCreationService : IFolderTagCreationService
{
    private readonly IFolderTagCreationHandler _handler;

    public FolderTagCreationService(IFolderTagCreationHandler handler)
    {
        _handler = handler;
    }

    public async Task<string?> AddRootFolderAsync()
    {
        var name = await _handler.ShowFolderDialogAsync("Добавить папку").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
            return null;
        _handler.AddRootFolder(name.Trim());
        _handler.Refresh();
        return name.Trim();
    }

    public async Task<string?> AddSubfolderAsync(string parentPath)
    {
        var name = await _handler.ShowFolderDialogAsync("Вложенная папка").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
            return null;
        _handler.AddSubfolder(parentPath ?? "", name.Trim());
        _handler.Refresh();
        return name.Trim();
    }

    public async Task<TagCreationResult?> CreateTagAsync(string dialogTitle)
    {
        var result = await _handler.ShowTagDialogAsync(dialogTitle).ConfigureAwait(true);
        if (result == null)
            return null;
        _handler.AddKnownTag(result.Name, result.ColorKey);
        _handler.Refresh();
        return result;
    }
}
