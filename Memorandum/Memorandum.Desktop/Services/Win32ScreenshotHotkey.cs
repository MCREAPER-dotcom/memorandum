using System;
using System.Runtime.InteropServices;

namespace Memorandum.Desktop.Services;

public static class Win32ScreenshotHotkey
{
    private static IntPtr _originalWndProc = IntPtr.Zero;
    private static IntPtr _hwndSubclass = IntPtr.Zero;
    private static Action? _onHotkey;
    private static readonly object Lock = new();

    private const int WM_HOTKEY = 0x0312;
    private const int MOD_WIN = 0x0008;
    private const int MOD_SHIFT = 0x0001;
    private const int GWLP_WNDPROC = -4;
    private const int HOTKEY_ID = 1;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static WndProcDelegate? _ourWndProc;
    private static GCHandle _wndProcHandle;

    public static bool TryRegister(IntPtr windowHandle, Action onHotkeyPressed)
    {
        if (windowHandle == IntPtr.Zero || onHotkeyPressed == null)
            return false;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        lock (Lock)
        {
            if (_hwndSubclass != IntPtr.Zero)
                return true;

            _onHotkey = onHotkeyPressed;
            _ourWndProc = SubclassWndProc;
            _wndProcHandle = GCHandle.Alloc(_ourWndProc);

            _originalWndProc = SetWindowLongPtr(windowHandle, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_ourWndProc));

            if (_originalWndProc == IntPtr.Zero)
            {
                _wndProcHandle.Free();
                return false;
            }

            if (!RegisterHotKey(windowHandle, HOTKEY_ID, (uint)(MOD_WIN | MOD_SHIFT), 0x53))
            {
                SetWindowLongPtr(windowHandle, GWLP_WNDPROC, _originalWndProc);
                _wndProcHandle.Free();
                return false;
            }

            _hwndSubclass = windowHandle;
            return true;
        }
    }

    public static void Unregister(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero) return;
        lock (Lock)
        {
            if (_hwndSubclass != windowHandle) return;
            UnregisterHotKey(windowHandle, HOTKEY_ID);
            if (_originalWndProc != IntPtr.Zero)
                SetWindowLongPtr(windowHandle, GWLP_WNDPROC, _originalWndProc);
            _originalWndProc = IntPtr.Zero;
            _hwndSubclass = IntPtr.Zero;
            _onHotkey = null;
            if (_wndProcHandle.IsAllocated)
                _wndProcHandle.Free();
        }
    }

    private static IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            try
            {
                _onHotkey?.Invoke();
            }
            catch
            {
                // ignore
            }
            return IntPtr.Zero;
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }
}
