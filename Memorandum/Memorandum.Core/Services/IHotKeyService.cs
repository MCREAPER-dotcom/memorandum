using System;

namespace Memorandum.Core.Services
{
    /// <summary>
    /// Регистрация и обработка глобальных горячих клавиш.
    /// </summary>
    public interface IHotKeyService
    {
        /// <summary>
        /// Регистрирует сочетание клавиш и действие при нажатии.
        /// </summary>
        /// <param name="id">Уникальный идентификатор хоткея.</param>
        /// <param name="modifiers">Модификаторы (Alt, Ctrl, Shift, Win).</param>
        /// <param name="key">Код клавиши.</param>
        /// <param name="callback">Действие при нажатии.</param>
        void Register(string id, HotKeyModifiers modifiers, int key, Action callback);

        /// <summary>
        /// Снимает регистрацию хоткея.
        /// </summary>
        void Unregister(string id);

        /// <summary>
        /// Снимает все зарегистрированные хоткеи.
        /// </summary>
        void UnregisterAll();
    }

    [Flags]
    public enum HotKeyModifiers
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        Win = 8
    }
}
