using System.Collections.Generic;
using System.Linq;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Реализация персистентности заметок: загрузка/сохранение через notes.json.
/// </summary>
public sealed class NotesPersistenceService : INotesPersistenceService
{
    public IReadOnlyList<NoteStorageDto> Load()
    {
        return NoteStorage.Load();
    }

    public void Save(IReadOnlyList<NoteCardItem> notes)
    {
        var dtos = notes.Select(n => n.ToStorageDto()).ToList();
        NoteStorage.Save(dtos);
    }
}
