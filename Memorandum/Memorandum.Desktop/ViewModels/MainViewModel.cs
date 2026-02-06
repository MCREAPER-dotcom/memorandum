using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.ViewModels;

public enum MainPageKind
{
    Notes,
    Graph,
    Presets,
    Settings
}

public sealed partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private MainPageKind _currentPage = MainPageKind.Notes;

    public NotesListViewModel NotesList { get; }
    public ObservableCollection<FolderItemViewModel> FolderItems { get; } = new();
    public ObservableCollection<TagItemViewModel> TagItems { get; } = new();

    private readonly List<string> _folderPaths = new(MainViewModelConstants.SidebarFolderNames);
    private readonly List<string> _knownTagNames = new();
    private readonly Dictionary<string, string> _tagColorKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly NotesGraphDataProvider _graphDataProvider;

    private string? _selectedFolder;
    private string? _selectedTag;

    public Action<NoteCardItem?>? ShowNoteEditRequested { get; set; }
    public Action<PresetItem?>? ShowNoteEditWithPresetRequested { get; set; }
    public Action<NoteCardItem>? OpenStickerRequested { get; set; }
    public Action<NoteCardItem>? CloseStickerRequested { get; set; }
    public Func<System.Threading.Tasks.Task<string?>>? ShowSubfolderDialogRequested { get; set; }
    public Action? RefreshSidebarRequested { get; set; }

    public MainViewModel()
    {
        var notesPersistence = new NotesPersistenceService();
        NotesList = new NotesListViewModel(notesPersistence);
        _graphDataProvider = new NotesGraphDataProvider(() => NotesList.GetAllNotes());

        var appState = AppStateStorage.Load();
        _folderPaths.Clear();
        if (appState.FolderPaths.Count > 0)
            _folderPaths.AddRange(appState.FolderPaths);
        else
            _folderPaths.AddRange(MainViewModelConstants.SidebarFolderNames);
        foreach (var kv in appState.TagColorKeys)
            _tagColorKeys[kv.Key] = kv.Value;

        NotesList.OnEditRequested = note => ShowNoteEditRequested?.Invoke(note);
        NotesList.OnOpenStickerRequested = note =>
        {
            if (note.IsSticker)
                OpenStickerRequested?.Invoke(note);
        };
        NotesList.OnStartTimerRequested = note =>
        {
            if (note.IsSticker)
                OpenStickerRequested?.Invoke(note);
        };
        NotesList.OnPauseTimerRequested = note => CloseStickerRequested?.Invoke(note);
    }

    [RelayCommand]
    private void NavigateToNotes()
    {
        _selectedFolder = null;
        _selectedTag = null;
        NotesList.SetFilter(null, null);
        CurrentPage = MainPageKind.Notes;
        RefreshSidebarRequested?.Invoke();
    }

    [RelayCommand]
    private void NavigateToGraph()
    {
        CurrentPage = MainPageKind.Graph;
        RefreshSidebarRequested?.Invoke();
    }

    [RelayCommand]
    private void CreateNote()
    {
        ShowNoteEditRequested?.Invoke(null);
    }

    [RelayCommand]
    private void NavigateToPresets()
    {
        CurrentPage = MainPageKind.Presets;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPage = MainPageKind.Settings;
    }

    public void SelectFolder(string? path)
    {
        _selectedFolder = _selectedFolder == path ? null : path;
        NotesList.SetFilter(_selectedFolder, _selectedTag);
        RefreshSidebarRequested?.Invoke();
    }

    private void SelectTag(string? name)
    {
        _selectedTag = _selectedTag == name ? null : name;
        NotesList.SetFilter(_selectedFolder, _selectedTag);
        RefreshSidebarRequested?.Invoke();
    }

    public async void AddSubfolder(string parentPath)
    {
        var name = ShowSubfolderDialogRequested != null ? await ShowSubfolderDialogRequested().ConfigureAwait(true) : null;
        if (string.IsNullOrWhiteSpace(name)) return;
        var newPath = string.IsNullOrEmpty(parentPath) ? name.Trim() : parentPath + "/" + name.Trim();
        if (!_folderPaths.Contains(newPath, StringComparer.OrdinalIgnoreCase))
        {
            _folderPaths.Add(newPath);
            RefreshSidebarRequested?.Invoke();
        }
    }

    public void AddRootFolder(string name)
    {
        var trimmed = (name ?? "").Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        if (!_folderPaths.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            _folderPaths.Add(trimmed);
            SaveAppState();
            RefreshSidebarRequested?.Invoke();
        }
    }

    public void AddSubfolderWithName(string parentPath, string name)
    {
        var trimmed = (name ?? "").Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        var newPath = string.IsNullOrEmpty(parentPath) ? trimmed : parentPath + "/" + trimmed;
        if (!_folderPaths.Contains(newPath, StringComparer.OrdinalIgnoreCase))
        {
            _folderPaths.Add(newPath);
            SaveAppState();
            RefreshSidebarRequested?.Invoke();
        }
    }

    public void AddKnownTag(string name, string? colorKey = null)
    {
        var trimmed = (name ?? "").Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        if (_knownTagNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(colorKey))
                _tagColorKeys[trimmed] = colorKey;
            SaveAppState();
            RefreshSidebarRequested?.Invoke();
            return;
        }
        _knownTagNames.Add(trimmed);
        if (!string.IsNullOrEmpty(colorKey))
            _tagColorKeys[trimmed] = colorKey;
        SaveAppState();
        RefreshSidebarRequested?.Invoke();
    }

    private void SaveAppState()
    {
        AppStateStorage.Save(_folderPaths, _tagColorKeys);
    }

    public string? GetTagColorKey(string tagName)
    {
        return _tagColorKeys.TryGetValue(tagName ?? "", out var key) ? key : null;
    }

    public IReadOnlyDictionary<string, string> GetTagColorKeys()
    {
        return new Dictionary<string, string>(_tagColorKeys);
    }

    public IReadOnlyList<string> GetFolderPaths() => _folderPaths;
    public IReadOnlyDictionary<string, int> GetFolderCounts() => NotesList.GetFolderCounts();
    public IReadOnlyDictionary<string, int> GetTagCounts() => NotesList.GetTagCounts();

    public IReadOnlyList<FolderRowForEdit> GetFoldersForEdit()
    {
        var counts = NotesList.GetFolderCounts();
        var ordered = OrderFolderPaths(_folderPaths);
        var list = new List<FolderRowForEdit>();
        foreach (var path in ordered)
        {
            var depth = path.Split('/').Length - 1;
            var displayName = path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;
            var count = counts.GetValueOrDefault(path, 0);
            list.Add(new FolderRowForEdit(path, displayName, depth, count));
        }
        return list;
    }

    public IReadOnlyList<string> GetTagNamesForEdit()
    {
        var counts = NotesList.GetTagCounts();
        return MainViewModelConstants.SidebarTagNames
            .Union(_knownTagNames, StringComparer.OrdinalIgnoreCase)
            .Union(counts.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(t =>
            {
                var i = Array.FindIndex(MainViewModelConstants.SidebarTagNames, s => string.Equals(s, t, StringComparison.OrdinalIgnoreCase));
                return i < 0 ? 999 : i;
            })
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Восстанавливает список папок из заметок: все пути из FolderName и все родительские сегменты (тест/тест1 → тест, тест/тест1).
    /// После перезапуска приложения иерархия папок на главной странице совпадает с графом.
    /// </summary>
    public void SyncFolderPathsFromNotes()
    {
        var fromNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var note in NotesList.GetAllNotes())
        {
            var fn = (note.FolderName ?? "").Trim();
            if (string.IsNullOrEmpty(fn)) continue;
            fromNotes.Add(fn);
            for (var i = 0; i < fn.Length; i++)
                if (fn[i] == '/')
                    fromNotes.Add(fn[..i]);
        }
        foreach (var path in fromNotes)
        {
            if (!_folderPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                _folderPaths.Add(path);
        }
    }

    public void RefreshFolderItems()
    {
        SyncFolderPathsFromNotes();
        var counts = NotesList.GetFolderCounts();
        var ordered = OrderFolderPaths(_folderPaths);
        FolderItems.Clear();
        foreach (var path in ordered)
        {
            var depth = path.Split('/').Length - 1;
            var displayName = path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;
            var count = counts.GetValueOrDefault(path, 0);
            var isSelected = path == _selectedFolder;
            var pathCopy = path;
            FolderItems.Add(new FolderItemViewModel(path, displayName, count, depth, isSelected,
                new RelayCommand(() => SelectFolder(pathCopy)),
                new RelayCommand(() => AddSubfolder(pathCopy))));
        }
    }

    public void RefreshTagItems()
    {
        var counts = NotesList.GetTagCounts();
        var allNames = MainViewModelConstants.SidebarTagNames
            .Union(_knownTagNames, StringComparer.OrdinalIgnoreCase)
            .Union(counts.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(t =>
            {
                var i = Array.FindIndex(MainViewModelConstants.SidebarTagNames, s => string.Equals(s, t, StringComparison.OrdinalIgnoreCase));
                return i < 0 ? 999 : i;
            })
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
        TagItems.Clear();
        foreach (var name in allNames)
        {
            var count = counts.GetValueOrDefault(name, 0);
            var isSelected = string.Equals(name, _selectedTag, StringComparison.OrdinalIgnoreCase);
            var nameCopy = name;
            var colorKey = _tagColorKeys.TryGetValue(name, out var ck) ? ck : null;
            TagItems.Add(new TagItemViewModel(name, count, isSelected, new RelayCommand(() => SelectTag(nameCopy)), colorKey));
        }
    }

    private static List<string> OrderFolderPaths(List<string> paths)
    {
        var roots = paths.Where(p => !p.Contains('/')).OrderBy(p =>
        {
            var i = Array.IndexOf(MainViewModelConstants.SidebarFolderNames, p);
            return i < 0 ? 999 : i;
        }).ThenBy(p => p).ToList();
        var result = new List<string>();
        foreach (var r in roots)
        {
            result.Add(r);
            result.AddRange(paths.Where(p => p.StartsWith(r + "/", StringComparison.Ordinal)).OrderBy(p => p));
        }
        result.AddRange(paths.Where(p => !result.Contains(p)).OrderBy(p => p));
        return result;
    }
}

internal static class MainViewModelConstants
{
    public static readonly string[] SidebarFolderNames = { "Работа", "Проекты", "Личное", "Архив" };
    public static readonly string[] SidebarTagNames = { "важное", "встреча", "покупки", "идеи", "разработка" };
}
