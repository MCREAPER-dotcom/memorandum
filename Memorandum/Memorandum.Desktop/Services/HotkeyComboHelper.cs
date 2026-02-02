using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Преобразование между строкой сочетания клавиш ("Ctrl+Alt+M") и Win32 mod/vk для RegisterHotKey.
/// </summary>
public static class HotkeyComboHelper
{
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private static readonly Dictionary<string, uint> ModifierNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Ctrl", MOD_CONTROL }, { "Control", MOD_CONTROL },
        { "Alt", MOD_ALT },
        { "Shift", MOD_SHIFT },
        { "Win", MOD_WIN }, { "Windows", MOD_WIN }
    };

    private static readonly Dictionary<string, uint> KeyToVk = new(StringComparer.OrdinalIgnoreCase);

    static HotkeyComboHelper()
    {
        for (var c = 'A'; c <= 'Z'; c++)
            KeyToVk[c.ToString()] = (uint)(0x41 + (c - 'A'));
        for (var c = '0'; c <= '9'; c++)
            KeyToVk[c.ToString()] = (uint)(0x30 + (c - '0'));
        KeyToVk["F1"] = 0x70; KeyToVk["F2"] = 0x71; KeyToVk["F3"] = 0x72; KeyToVk["F4"] = 0x73;
        KeyToVk["F5"] = 0x74; KeyToVk["F6"] = 0x75; KeyToVk["F7"] = 0x76; KeyToVk["F8"] = 0x77;
        KeyToVk["F9"] = 0x78; KeyToVk["F10"] = 0x79; KeyToVk["F11"] = 0x7A; KeyToVk["F12"] = 0x7B;
        KeyToVk["Space"] = 0x20; KeyToVk["Return"] = 0x0D; KeyToVk["Escape"] = 0x1B;
        KeyToVk["Tab"] = 0x09; KeyToVk["Insert"] = 0x2D; KeyToVk["Delete"] = 0x2E;
        KeyToVk["Home"] = 0x24; KeyToVk["End"] = 0x23; KeyToVk["PageUp"] = 0x21; KeyToVk["PageDown"] = 0x22;
    }

    /// <summary>
    /// Парсит строку "Ctrl+Alt+M" в (modifiers, virtualKey) для Win32.
    /// </summary>
    public static bool TryParseToWin32(string? keyCombo, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;
        if (string.IsNullOrWhiteSpace(keyCombo)) return false;
        var parts = keyCombo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        var last = parts[^1];
        if (!KeyToVk.TryGetValue(last, out vk))
        {
            if (last.Length == 1)
            {
                var ch = char.ToUpperInvariant(last[0]);
                if (ch >= 'A' && ch <= 'Z') vk = (uint)(0x41 + (ch - 'A'));
                else if (ch >= '0' && ch <= '9') vk = (uint)(0x30 + (ch - '0'));
                else return false;
            }
            else return false;
        }
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (ModifierNames.TryGetValue(parts[i], out var mod))
                modifiers |= mod;
        }
        return true;
    }

    /// <summary>
    /// Форматирует (modifiers, vk) в строку "Ctrl+Alt+M".
    /// </summary>
    public static string FormatFromWin32(uint modifiers, uint vk)
    {
        var parts = new List<string>();
        if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
        var keyStr = VkToKeyString(vk);
        if (!string.IsNullOrEmpty(keyStr)) parts.Add(keyStr);
        return parts.Count > 0 ? string.Join("+", parts) : "";
    }

    private static string VkToKeyString(uint vk)
    {
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
        if (vk >= 0x70 && vk <= 0x7B) return "F" + (vk - 0x70 + 1);
        if (vk == 0x20) return "Space";
        if (vk == 0x0D) return "Return";
        if (vk == 0x1B) return "Escape";
        if (vk == 0x09) return "Tab";
        if (vk == 0x2D) return "Insert";
        if (vk == 0x2E) return "Delete";
        if (vk == 0x24) return "Home";
        if (vk == 0x23) return "End";
        if (vk == 0x21) return "PageUp";
        if (vk == 0x22) return "PageDown";
        return "";
    }

    /// <summary>
    /// Преобразует Avalonia Key и KeyModifiers в строку "Ctrl+Alt+M".
    /// </summary>
    public static string FromAvalonia(Key key, KeyModifiers modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & KeyModifiers.Control) != 0) parts.Add("Ctrl");
        if ((modifiers & KeyModifiers.Alt) != 0) parts.Add("Alt");
        if ((modifiers & KeyModifiers.Shift) != 0) parts.Add("Shift");
        if ((modifiers & KeyModifiers.Meta) != 0) parts.Add("Win");
        var keyStr = KeyToDisplayString(key);
        if (!string.IsNullOrEmpty(keyStr)) parts.Add(keyStr);
        return parts.Count > 0 ? string.Join("+", parts) : "";
    }

    /// <summary>
    /// Преобразует Avalonia Key и KeyModifiers в (mod, vk) для Win32.
    /// </summary>
    public static bool FromAvaloniaToWin32(Key key, KeyModifiers modifiers, out uint mod, out uint vk)
    {
        mod = 0;
        if ((modifiers & KeyModifiers.Control) != 0) mod |= MOD_CONTROL;
        if ((modifiers & KeyModifiers.Alt) != 0) mod |= MOD_ALT;
        if ((modifiers & KeyModifiers.Shift) != 0) mod |= MOD_SHIFT;
        if ((modifiers & KeyModifiers.Meta) != 0) mod |= MOD_WIN;
        vk = AvaloniaKeyToVk(key);
        return vk != 0;
    }

    private static uint AvaloniaKeyToVk(Key key)
    {
        var name = key.ToString();
        if (string.IsNullOrEmpty(name)) return 0;
        if (KeyToVk.TryGetValue(name, out var vk)) return vk;
        if (name.Length == 1)
        {
            var c = char.ToUpperInvariant(name[0]);
            if (c >= 'A' && c <= 'Z') return (uint)(0x41 + (c - 'A'));
            if (c >= '0' && c <= '9') return (uint)(0x30 + (c - '0'));
        }
        return 0;
    }

    private static string KeyToDisplayString(Key key)
    {
        var name = key.ToString();
        if (string.IsNullOrEmpty(name)) return "";
        if (name.Length == 1) return name.ToUpperInvariant();
        if (KeyToVk.ContainsKey(name)) return name;
        return name;
    }
}
