using System;

namespace Memorandum.Core.Entities
{
    /// <summary>
    /// Папка для распределения заметок.
    /// </summary>
    public sealed class Folder
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        /// <summary>
        /// Порядок отображения среди соседних папок.
        /// </summary>
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }

        public Folder()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            ParentId = null;
            Order = 0;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
