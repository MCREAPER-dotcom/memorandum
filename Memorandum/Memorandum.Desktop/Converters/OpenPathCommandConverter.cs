using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using Avalonia.Data.Converters;

namespace Memorandum.Desktop.Converters;

/// <summary>
/// Преобразует путь к файлу/папке в ICommand, открывающий его в системе (проводник / приложение по умолчанию).
/// </summary>
public class OpenPathCommandConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string raw)
            return NullCommand.Instance;
        var path = raw.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return NullCommand.Instance;
        return new OpenPathCommand(path);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        null;

    private sealed class OpenPathCommand : ICommand
    {
        private readonly string _path;

        public OpenPathCommand(string path) => _path = path;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            var path = _path.Trim();
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { /* игнорируем ошибки открытия */ }
        }

        public event EventHandler? CanExecuteChanged;
    }

    private sealed class NullCommand : ICommand
    {
        public static readonly NullCommand Instance = new();
        public bool CanExecute(object? parameter) => false;
        public void Execute(object? parameter) { }
        public event EventHandler? CanExecuteChanged;
    }
}
