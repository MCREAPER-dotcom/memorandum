namespace Memorandum.Desktop.Models;

/// <summary>
/// DTO для сериализации дополнительных настроек приложения в settings.json.
/// </summary>
public class AppSettingsDto
{
    public string NotificationSoundPath { get; set; } = "notification.mp3";
    public string BackupPath { get; set; } = "";
    public bool RunAtStartup { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool StickerAnimation { get; set; } = true;
}
