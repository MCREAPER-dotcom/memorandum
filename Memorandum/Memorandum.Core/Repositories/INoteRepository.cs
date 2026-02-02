using System;
using System.Collections.Generic;
using Memorandum.Core.Entities;

namespace Memorandum.Core.Repositories
{
    /// <summary>
    /// Персистентность заметок: CRUD и выборка по папке/тегам.
    /// </summary>
    public interface INoteRepository
    {
        Note GetById(Guid id);
        IReadOnlyList<Note> GetAll();
        IReadOnlyList<Note> GetByFolderId(Guid? folderId);
        IReadOnlyList<Note> GetByTagId(Guid tagId);
        IReadOnlyList<Note> GetByType(NoteType noteType);

        void Add(Note note);
        void Update(Note note);
        void Delete(Guid id);
        bool Exists(Guid id);
    }
}
