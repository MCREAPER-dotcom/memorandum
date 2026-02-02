using System;

namespace Memorandum.Core.Entities
{
    /// <summary>
    /// Тег для группировки и поиска заметок.
    /// </summary>
    public sealed class Tag
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Цвет в формате #RRGGBB (опционально).
        /// </summary>
        public string Color { get; set; }

        public Tag()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            Color = string.Empty;
        }
    }
}
