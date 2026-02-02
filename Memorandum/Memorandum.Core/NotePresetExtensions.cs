using Memorandum.Core.Entities;

namespace Memorandum.Core
{
    /// <summary>
    /// Расширения для применения пресета к заметке (DRY: одна точка копирования настроек).
    /// </summary>
    public static class NotePresetExtensions
    {
        /// <summary>
        /// Копирует визуальные и поведенческие настройки из пресета в заметку.
        /// </summary>
        public static void ApplyPreset(this Note note, NotePreset preset)
        {
            if (preset == null) return;

            note.NoteType = preset.NoteType;
            note.BackgroundColor = preset.BackgroundColor;
            note.TextColor = preset.TextColor;
            note.BackgroundOpacity = preset.BackgroundOpacity;
            note.ContentOpacity = preset.ContentOpacity;
            note.CloseTrigger = preset.CloseTrigger;
            note.LifetimeMinutes = preset.LifetimeMinutes;
            note.TimerDurationSeconds = preset.TimerDurationSeconds;
            note.CompletionSoundPath = preset.CompletionSoundPath;
            note.CompletionSoundVolume = preset.CompletionSoundVolume;
            note.Width = preset.Width;
            note.Height = preset.Height;
            note.PresetId = preset.Id;
        }
    }
}
