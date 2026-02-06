using System;
using System.Threading.Tasks;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Реализация <see cref="IFolderTagCreationHandler"/> на делегатах.
/// Создаётся в одном месте (например MainWindow) и передаётся в <see cref="FolderTagCreationService"/>.
/// </summary>
public sealed class FolderTagCreationHandler : IFolderTagCreationHandler
{
    private readonly Func<string, Task<string?>> _showFolderDialog;
    private readonly Func<string, Task<TagCreationResult?>> _showTagDialog;
    private readonly Action<string> _addRootFolder;
    private readonly Action<string, string> _addSubfolder;
    private readonly Action<string, string?> _addKnownTag;
    private readonly Action _refresh;

    public FolderTagCreationHandler(
        Func<string, Task<string?>> showFolderDialog,
        Func<string, Task<TagCreationResult?>> showTagDialog,
        Action<string> addRootFolder,
        Action<string, string> addSubfolder,
        Action<string, string?> addKnownTag,
        Action refresh)
    {
        _showFolderDialog = showFolderDialog ?? throw new ArgumentNullException(nameof(showFolderDialog));
        _showTagDialog = showTagDialog ?? throw new ArgumentNullException(nameof(showTagDialog));
        _addRootFolder = addRootFolder ?? throw new ArgumentNullException(nameof(addRootFolder));
        _addSubfolder = addSubfolder ?? throw new ArgumentNullException(nameof(addSubfolder));
        _addKnownTag = addKnownTag ?? throw new ArgumentNullException(nameof(addKnownTag));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
    }

    public Task<string?> ShowFolderDialogAsync(string title) => _showFolderDialog(title);
    public Task<TagCreationResult?> ShowTagDialogAsync(string title) => _showTagDialog(title);
    public void AddRootFolder(string name) => _addRootFolder(name);
    public void AddSubfolder(string parentPath, string name) => _addSubfolder(parentPath, name);
    public void AddKnownTag(string name, string? colorKey) => _addKnownTag(name, colorKey);
    public void Refresh() => _refresh();
}
