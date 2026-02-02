using System;
using System.Collections.Generic;

namespace Memorandum.Core.Entities
{
    /// <summary>
    /// Заметка: текст, ссылки, изображения, время редактирования, настройки отображения и таймера.
    /// </summary>
    public sealed class Note
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        /// <summary>
        /// Основной текст заметки.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Гиперссылки (URL) в заметке.
        /// </summary>
        public List<string> Hyperlinks { get; set; }
        /// <summary>
        /// Пути к изображениям или идентификаторы в хранилище (реализация в Infrastructure).
        /// </summary>
        public List<string> ImagePaths { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastEditedAt { get; set; }

        public Guid? FolderId { get; set; }
        public List<Guid> TagIds { get; set; }

        public NoteType NoteType { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public double BackgroundOpacity { get; set; }
        public double ContentOpacity { get; set; }

        public CloseTriggerKind CloseTrigger { get; set; }
        public int? LifetimeMinutes { get; set; }
        public int? TimerDurationSeconds { get; set; }

        public string CompletionSoundPath { get; set; }
        public double CompletionSoundVolume { get; set; }

        /// <summary>
        /// Позиция окна-стикера по X (для NoteType.Sticker).
        /// </summary>
        public double? PositionX { get; set; }
        public double? PositionY { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }

        /// <summary>
        /// Идентификатор пресета, из которого создана заметка (если применимо).
        /// </summary>
        public Guid? PresetId { get; set; }

        public Note()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            Content = string.Empty;
            Hyperlinks = new List<string>();
            ImagePaths = new List<string>();
            TagIds = new List<Guid>();
            CreatedAt = DateTime.UtcNow;
            LastEditedAt = DateTime.UtcNow;
            NoteType = NoteType.Plain;
            BackgroundColor = "#FFFFFF";
            TextColor = "#000000";
            BackgroundOpacity = 1.0;
            ContentOpacity = 1.0;
            CloseTrigger = CloseTriggerKind.Manual;
            CompletionSoundPath = string.Empty;
            CompletionSoundVolume = 0.8;
        }
    }
}
