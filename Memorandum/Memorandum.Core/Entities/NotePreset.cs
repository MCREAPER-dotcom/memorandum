using System;

namespace Memorandum.Core.Entities
{
    /// <summary>
    /// Пресет настроек заметки: внешний вид, таймер, звук. Используется как шаблон при создании заметки или стикера.
    /// </summary>
    public sealed class NotePreset
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public NoteType NoteType { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public double BackgroundOpacity { get; set; }
        public double ContentOpacity { get; set; }

        public CloseTriggerKind CloseTrigger { get; set; }
        /// <summary>
        /// Время жизни в минутах (при CloseTrigger = Timer).
        /// </summary>
        public int? LifetimeMinutes { get; set; }

        /// <summary>
        /// Длительность таймера в заметке (опционально).
        /// </summary>
        public int? TimerDurationSeconds { get; set; }

        public string CompletionSoundPath { get; set; }
        public double CompletionSoundVolume { get; set; }

        public double? Width { get; set; }
        public double? Height { get; set; }

        public DateTime CreatedAt { get; set; }

        public NotePreset()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            NoteType = NoteType.Plain;
            BackgroundColor = "#FFFFFF";
            TextColor = "#000000";
            BackgroundOpacity = 1.0;
            ContentOpacity = 1.0;
            CloseTrigger = CloseTriggerKind.Manual;
            CompletionSoundPath = string.Empty;
            CompletionSoundVolume = 0.8;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
