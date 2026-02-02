using System;
using System.Collections.Generic;
using Memorandum.Core.Entities;

namespace Memorandum.Core.Repositories
{
    /// <summary>
    /// Персистентность папок: CRUD и иерархия.
    /// </summary>
    public interface IFolderRepository
    {
        Folder GetById(Guid id);
        IReadOnlyList<Folder> GetAll();
        IReadOnlyList<Folder> GetByParentId(Guid? parentId);

        void Add(Folder folder);
        void Update(Folder folder);
        void Delete(Guid id);
        bool Exists(Guid id);
    }
}
