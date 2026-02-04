using System.Collections.Generic;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Загрузка и сохранение заметок (отдельно от логики списка и UI).
/// </summary>
public interface INotesPersistenceService
{
    IReadOnlyList<NoteStorageDto> Load();
    void Save(IReadOnlyList<NoteCardItem> notes);
}
