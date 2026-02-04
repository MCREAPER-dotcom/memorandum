using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.ViewModels;

public sealed partial class NotesListViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _foundCountText = "Найдено заметок: 0";

    public ObservableCollection<NoteCardViewModel> FilteredNotes { get; } = new();
    private readonly List<NoteCardItem> _allNotes = new();
    private string? _selectedFolder;
    private string? _selectedTag;

    public IReadOnlyList<NoteCardItem> GetAllNotes() => _allNotes;
    public IReadOnlyDictionary<string, int> GetFolderCounts() => CountBy(_allNotes, n => n.FolderName ?? "");
    public IReadOnlyDictionary<string, int> GetTagCounts()
    {
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in _allNotes)
            foreach (var tag in n.TagLabels)
                d[tag] = d.GetValueOrDefault(tag, 0) + 1;
        return d;
    }

    public Action<NoteCardItem>? OnEditRequested { get; set; }
    public Action<NoteCardItem>? OnOpenStickerRequested { get; set; }
    public Action<NoteCardItem>? OnStartTimerRequested { get; set; }
    public Action<NoteCardItem>? OnPauseTimerRequested { get; set; }

    private readonly INotesPersistenceService _persistence;

    public NotesListViewModel(INotesPersistenceService persistence)
    {
        _persistence = persistence;
        LoadNotes();
    }

    public void SetFilter(string? folder, string? tag)
    {
        _selectedFolder = folder;
        _selectedTag = tag;
        ApplyFilter();
    }

    public void RefreshFilter(string? folder = null, string? tag = null)
    {
        SetFilter(folder ?? _selectedFolder, tag ?? _selectedTag);
    }

    /// <summary>
    /// Поиск по названию заметки: при подтверждении (Enter) найденная заметка поднимается вверх и подсвечивается.
    /// </summary>
    public void SearchAndHighlight(string query)
    {
        var q = query?.Trim();
        if (string.IsNullOrEmpty(q))
        {
            ApplyFilter(null);
            return;
        }
        var filtered = _allNotes.AsEnumerable();
        if (!string.IsNullOrEmpty(_selectedFolder))
            filtered = filtered.Where(n => n.FolderName == _selectedFolder);
        if (!string.IsNullOrEmpty(_selectedTag))
            filtered = filtered.Where(n => n.TagLabels.Contains(_selectedTag!, StringComparer.OrdinalIgnoreCase));
        var list = filtered.ToList();
        var found = list.FirstOrDefault(n => n.Title.Contains(q, StringComparison.OrdinalIgnoreCase));
        ApplyFilter(found);
    }

    public void AddNote(NoteCardItem item)
    {
        _allNotes.Insert(0, item);
        ApplyFilter();
        SaveNotes();
    }

    public void ReplaceNote(NoteCardItem oldItem, NoteCardItem newItem)
    {
        var idx = _allNotes.IndexOf(oldItem);
        if (idx >= 0)
        {
            _allNotes[idx] = newItem;
            ApplyFilter();
            SaveNotes();
        }
    }

    public void RemoveNote(NoteCardItem item)
    {
        var idx = _allNotes.IndexOf(item);
        if (idx >= 0)
        {
            _allNotes.RemoveAt(idx);
            ApplyFilter();
            SaveNotes();
        }
    }

    private void LoadNotes()
    {
        _allNotes.Clear();
        var dtos = _persistence.Load();
        foreach (var dto in dtos)
        {
            NoteCardItem? item = null;
            item = NoteCardItem.FromStorageDto(dto,
                () => OnEditRequested?.Invoke(item!),
                () => OnOpenStickerRequested?.Invoke(item!),
                () => OnStartTimerRequested?.Invoke(item!),
                () => OnPauseTimerRequested?.Invoke(item!));
            _allNotes.Add(item);
        }
        ApplyFilter();
    }

    private void SaveNotes()
    {
        _persistence.Save(_allNotes);
    }

    private void ApplyFilter(NoteCardItem? highlightNote = null)
    {
        var filtered = _allNotes.AsEnumerable();
        if (!string.IsNullOrEmpty(_selectedFolder))
            filtered = filtered.Where(n => n.FolderName == _selectedFolder);
        if (!string.IsNullOrEmpty(_selectedTag))
            filtered = filtered.Where(n => n.TagLabels.Contains(_selectedTag!, StringComparer.OrdinalIgnoreCase));
        var list = filtered.ToList();
        if (highlightNote != null)
        {
            list.Remove(highlightNote);
            list.Insert(0, highlightNote);
        }
        FilteredNotes.Clear();
        foreach (var note in list)
        {
            var vm = new NoteCardViewModel(note);
            if (note == highlightNote)
                vm.IsHighlighted = true;
            FilteredNotes.Add(vm);
        }
        FoundCountText = $"Найдено заметок: {FilteredNotes.Count}";
    }

    private static Dictionary<string, int> CountBy(List<NoteCardItem> notes, Func<NoteCardItem, string> keySelector)
    {
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in notes)
        {
            var key = keySelector(n) ?? "";
            d[key] = d.GetValueOrDefault(key, 0) + 1;
        }
        return d;
    }
}
