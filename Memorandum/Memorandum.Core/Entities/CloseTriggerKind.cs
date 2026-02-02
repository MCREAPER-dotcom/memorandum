namespace Memorandum.Core.Entities
{
    /// <summary>
    /// Условие закрытия заметки: по таймеру или вручную.
    /// </summary>
    public enum CloseTriggerKind
    {
        Manual = 0,
        Timer = 1
    }
}
