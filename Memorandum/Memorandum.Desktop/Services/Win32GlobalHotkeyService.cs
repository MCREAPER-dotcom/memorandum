using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

/// <summary>
/// Регистрирует несколько глобальных горячих клавиш Win32 и вызывает соответствующие действия.
/// Логика действий передаётся снаружи (привязка к функционалу приложения).
/// </summary>
public static class Win32GlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int GWLP_WNDPROC = -4;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr _originalWndProc = IntPtr.Zero;
    private static IntPtr _hwndSubclass = IntPtr.Zero;
    private static WndProcDelegate? _ourWndProc;
    private static GCHandle _wndProcHandle;
    private static readonly Dictionary<int, Action> ActionsById = new();
    private static readonly object Lock = new();

    /// <summary>
    /// Регистрирует горячие клавиши из списка. Вызовы action выполняются при нажатии соответствующей комбинации.
    /// </summary>
    public static bool RegisterAll(IntPtr windowHandle, IReadOnlyList<HotkeyConfigItem> config, IReadOnlyDictionary<string, Action> actionByActionId)
    {
        if (windowHandle == IntPtr.Zero || config == null || actionByActionId == null)
            return false;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        lock (Lock)
        {
            UnregisterAll(windowHandle);

            _ourWndProc = SubclassWndProc;
            _wndProcHandle = GCHandle.Alloc(_ourWndProc);
            _originalWndProc = SetWindowLongPtr(windowHandle, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_ourWndProc));
            if (_originalWndProc == IntPtr.Zero)
            {
                _wndProcHandle.Free();
                return false;
            }

            var id = 1;
            foreach (var item in config)
            {
                if (string.IsNullOrWhiteSpace(item.KeyCombo)) continue;
                if (!actionByActionId.TryGetValue(item.ActionId, out var action) || action == null) continue;
                if (!HotkeyComboHelper.TryParseToWin32(item.KeyCombo, out var mod, out var vk)) continue;
                if (!RegisterHotKey(windowHandle, id, mod, vk))
                    continue;
                ActionsById[id] = action;
                id++;
                if (id > 16) break;
            }

            _hwndSubclass = windowHandle;
            return true;
        }
    }

    public static void UnregisterAll(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero) return;
        lock (Lock)
        {
            if (_hwndSubclass != windowHandle) return;
            for (var id = 1; id <= 16; id++)
                UnregisterHotKey(windowHandle, id);
            ActionsById.Clear();
            if (_originalWndProc != IntPtr.Zero)
                SetWindowLongPtr(windowHandle, GWLP_WNDPROC, _originalWndProc);
            _originalWndProc = IntPtr.Zero;
            _hwndSubclass = IntPtr.Zero;
            if (_wndProcHandle.IsAllocated)
                _wndProcHandle.Free();
        }
    }

    private static IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            lock (Lock)
            {
                if (ActionsById.TryGetValue(id, out var action))
                {
                    try { action(); } catch { /* ignore */ }
                    return IntPtr.Zero;
                }
            }
        }
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }
}
