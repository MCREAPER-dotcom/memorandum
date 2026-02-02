using System;
using System.Collections.Generic;
using Memorandum.Core.Entities;

namespace Memorandum.Core.Repositories
{
    /// <summary>
    /// Персистентность тегов: CRUD и поиск по имени.
    /// </summary>
    public interface ITagRepository
    {
        Tag GetById(Guid id);
        IReadOnlyList<Tag> GetAll();
        Tag GetByName(string name);

        void Add(Tag tag);
        void Update(Tag tag);
        void Delete(Guid id);
        bool Exists(Guid id);
    }
}
