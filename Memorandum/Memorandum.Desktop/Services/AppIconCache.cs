using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Кэш иконки приложения: одна загрузка из файла, переиспользование во всех окнах.
/// </summary>
public static class AppIconCache
{
    private static WindowIcon? _icon;
    private static Bitmap? _bitmap;
    private static readonly object Lock = new();

    public static WindowIcon? Icon
    {
        get
        {
            EnsureLoaded();
            return _icon;
        }
    }

    public static Bitmap? Bitmap
    {
        get
        {
            EnsureLoaded();
            return _bitmap;
        }
    }

    /// <summary>Вызвать при старте приложения, чтобы иконка была загружена до первого окна.</summary>
    public static void Preload()
    {
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (_icon != null)
            return;
        lock (Lock)
        {
            if (_icon != null)
                return;
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Memorandum-AppIcon.png");
                if (File.Exists(path))
                {
                    _icon = new WindowIcon(path);
                    _bitmap = new Bitmap(path);
                }
            }
            catch
            {
                // иконка не задана
            }
        }
    }
}
