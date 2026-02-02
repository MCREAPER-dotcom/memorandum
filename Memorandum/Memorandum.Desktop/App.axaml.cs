using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Svg.Skia;

namespace Memorandum.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            var playSource = SvgSource.Load("avares://Memorandum.Desktop/Assets/Icons/play.svg", null);
            var pauseSource = SvgSource.Load("avares://Memorandum.Desktop/Assets/Icons/pause.svg", null);
            var fileSource = SvgSource.Load("avares://Memorandum.Desktop/Assets/Icons/file.svg", null);
            var searchSource = SvgSource.Load("avares://Memorandum.Desktop/Assets/Icons/search.svg", null);
            if (playSource != null)
                Resources["PlayIcon"] = new SvgImage { Source = playSource };
            if (pauseSource != null)
                Resources["PauseIcon"] = new SvgImage { Source = pauseSource };
            if (fileSource != null)
                Resources["FileIcon"] = new SvgImage { Source = fileSource };
            if (searchSource != null)
                Resources["SearchIcon"] = new SvgImage { Source = searchSource };
        }
        catch
        {
            // иконки таймера не загружены
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
