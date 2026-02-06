using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Models;

public class PresetItem
{
    public string Title { get; set; }
    public int TransparencyPercent { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsSticker { get; set; }
    public string ColorHex { get; set; }
    public string? FolderName { get; set; }
    public List<string> TagLabels { get; set; } = new();

    public string Details =>
        DurationMinutes.HasValue
            ? $"Прозрачность: {TransparencyPercent}%, {DurationMinutes} мин"
            : $"Прозрачность: {TransparencyPercent}%";

    public string TypeLabel => IsSticker ? "Стикер" : "Обычная";

    public string TagDisplayText => TagLabels.Count > 0 ? string.Join(", ", TagLabels) : "—";

    public IBrush ColorBrush => new SolidColorBrush(Avalonia.Media.Color.Parse(ColorHex));
    public IBrush TypeBackground
    {
        get
        {
            var key = IsSticker ? PaletteConstants.PresetTypeStickerBackgroundKey : PaletteConstants.PresetTypeNormalBackgroundKey;
            if (Avalonia.Application.Current?.Resources?.TryGetResource(key, null, out var value) == true && value is IBrush brush)
                return brush;
            var hex = IsSticker ? PaletteConstants.DefaultPresetTypeStickerHex : PaletteConstants.DefaultPresetTypeNormalHex;
            return new SolidColorBrush(Avalonia.Media.Color.Parse(hex));
        }
    }

    public PresetItem(string title, string details, string typeLabel, string colorHex)
    {
        Title = title;
        ColorHex = colorHex;
        IsSticker = typeLabel == "Стикер";
        ParseDetails(details);
    }

    public PresetItem(string title, int transparencyPercent, int? durationMinutes, bool isSticker, string colorHex, IReadOnlyList<string>? tagLabels = null, string? folderName = null)
    {
        Title = title;
        TransparencyPercent = transparencyPercent;
        DurationMinutes = durationMinutes;
        IsSticker = isSticker;
        ColorHex = colorHex;
        FolderName = folderName;
        if (tagLabels != null)
            TagLabels = tagLabels.ToList();
    }

    private void ParseDetails(string details)
    {
        TransparencyPercent = 100;
        DurationMinutes = null;
        if (string.IsNullOrEmpty(details)) return;
        var pct = System.Text.RegularExpressions.Regex.Match(details, @"(\d+)\s*%");
        if (pct.Success && int.TryParse(pct.Groups[1].Value, out var t))
            TransparencyPercent = t;
        var min = System.Text.RegularExpressions.Regex.Match(details, @"(\d+)\s*мин");
        if (min.Success && int.TryParse(min.Groups[1].Value, out var d))
            DurationMinutes = d;
    }
}
