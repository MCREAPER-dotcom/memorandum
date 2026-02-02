using System;
using System.Collections.Generic;
using Memorandum.Core.Entities;

namespace Memorandum.Core.Repositories
{
    /// <summary>
    /// Персистентность пресетов заметок.
    /// </summary>
    public interface IPresetRepository
    {
        NotePreset GetById(Guid id);
        IReadOnlyList<NotePreset> GetAll();

        void Add(NotePreset preset);
        void Update(NotePreset preset);
        void Delete(Guid id);
        bool Exists(Guid id);
    }
}
