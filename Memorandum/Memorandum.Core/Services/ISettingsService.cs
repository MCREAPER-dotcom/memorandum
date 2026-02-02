namespace Memorandum.Core.Services
{
    /// <summary>
    /// Хранение и чтение настроек приложения (ключ-значение).
    /// </summary>
    public interface ISettingsService
    {
        T Get<T>(string key, T defaultValue = default);
        void Set<T>(string key, T value);
        bool Contains(string key);
        void Remove(string key);
    }
}
