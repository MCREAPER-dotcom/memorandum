using System.Collections.Generic;

namespace Memorandum.Desktop.Themes;

/// <summary>
/// Ключи ресурсов палитры. Все цвета задаются в AXAML (PaletteApp, PaletteTags, PaletteGraph).
/// Здесь только имена ключей и маппинг тегов по умолчанию — без хардкода hex.
/// </summary>
public static class PaletteConstants
{
    /// <summary>Ключи кистей для тегов (порядок палитры выбора цвета).</summary>
    public static readonly string[] TagPillResourceKeys =
    {
        "TagPillPurple",
        "TagPillGreen",
        "TagPillRed",
        "TagPillCyan",
        "TagPillYellow",
        "TagPillOrange",
        "TagPillPink",
        "TagPillBlue",
        "TagPillIndigo",
        "TagPillTeal",
        "TagPillLime",
        "TagPillAmber",
        "TagPillRose",
        "TagPillSky",
        "TagPillViolet",
        "TagPillFuchsia",
        "TagPillEmerald",
        "TagPillSlate",
        "TagPillCoral",
        "TagPillMint"
    };

    /// <summary>Теги по умолчанию → ключ ресурса палитры тегов.</summary>
    public static readonly IReadOnlyDictionary<string, string> DefaultTagNameToKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "важное", "TagPillPurple" },
            { "встреча", "TagPillGreen" },
            { "покупки", "TagPillRed" },
            { "идеи", "TagPillCyan" },
            { "разработка", "TagPillYellow" }
        };

    /// <summary>Ключ кисти фона стикера по умолчанию (для fallback в коде).</summary>
    public const string StickerBackgroundKey = "StickerBackground";

    /// <summary>Ключи типа заметки на карточке (стикер / обычная).</summary>
    public const string NoteTypeStickerBackgroundKey = "NoteTypeStickerBackground";
    public const string NoteTypeNormalBackgroundKey = "NoteTypeNormalBackground";

    /// <summary>Ключи типа пресета в списке (стикер / обычная).</summary>
    public const string PresetTypeStickerBackgroundKey = "PresetTypeStickerBackground";
    public const string PresetTypeNormalBackgroundKey = "PresetTypeNormalBackground";

    /// <summary>Ключ акцентной кисти (для цвета пресета по умолчанию).</summary>
    public const string AccentBrushKey = "AccentBrush";

    /// <summary>Ключ кисти «опасное действие» (удаление).</summary>
    public const string DangerBrushKey = "TagPillRed";

    /// <summary>Hex фона стикера по умолчанию (совпадает с StickerBackground в палитре). Fallback для конвертера.</summary>
    public const string DefaultStickerBackgroundHex = "#fffde7";

    /// <summary>Hex для TypeBackground пресета (стикер), fallback при отсутствии ресурсов.</summary>
    public const string DefaultPresetTypeStickerHex = "#b45309";

    /// <summary>Hex для TypeBackground пресета (обычная), fallback при отсутствии ресурсов.</summary>
    public const string DefaultPresetTypeNormalHex = "#6b7280";

    /// <summary>Hex fallback для кисти тега, если ресурс недоступен (совпадает с TagPillPurple).</summary>
    public const string DefaultTagPillFallbackHex = "#5b21b6";

    /// <summary>Hex цвета пресета по умолчанию (совпадает с AccentBrush в палитре).</summary>
    public const string DefaultPresetColorHex = "#7c3aed";
}
