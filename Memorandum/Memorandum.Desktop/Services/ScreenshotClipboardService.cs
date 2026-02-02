using System.IO;
using System.Runtime.InteropServices;

namespace Memorandum.Desktop.Services;

public static class ScreenshotClipboardService
{
    /// <summary>
    /// Читает изображение из буфера обмена Windows (CF_DIB) и сохраняет в папку. Возвращает путь к файлу или null.
    /// </summary>
    public static string? GetImageFromClipboardAndSaveToFile(IntPtr windowHandle)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;
        try
        {
            if (!OpenClipboard(windowHandle))
                return null;
            try
            {
                IntPtr hMem = GetClipboardData(CF_DIB);
                if (hMem == IntPtr.Zero)
                    return null;
                IntPtr ptr = GlobalLock(hMem);
                if (ptr == IntPtr.Zero)
                    return null;
                try
                {
                    int headerSize = Marshal.ReadInt32(ptr, 0);
                    if (headerSize < 40)
                        return null;
                    int width = Marshal.ReadInt32(ptr, 4);
                    int heightRaw = Marshal.ReadInt32(ptr, 8);
                    int height = Math.Abs(heightRaw);
                    short planes = Marshal.ReadInt16(ptr, 12);
                    short bitCount = Marshal.ReadInt16(ptr, 14);
                    if (width <= 0 || height <= 0 || bitCount != 32)
                        return null;
                    int stride = width * 4;
                    int pixelDataOffset = headerSize;
                    int pixelDataSize = stride * height;
                    using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    var rect = new System.Drawing.Rectangle(0, 0, width, height);
                    var bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    try
                    {
                        var row = new byte[stride];
                        for (int y = 0; y < height; y++)
                        {
                            int srcY = height - 1 - y;
                            IntPtr srcRow = IntPtr.Add(ptr, pixelDataOffset + srcY * stride);
                            Marshal.Copy(srcRow, row, 0, stride);
                            Marshal.Copy(row, 0, IntPtr.Add(bmpData.Scan0, y * Math.Abs(bmpData.Stride)), stride);
                        }
                    }
                    finally
                    {
                        bitmap.UnlockBits(bmpData);
                    }
                    var dir = NoteAttachmentsHelper.GetAttachmentsFolder();
                    var fileName = $"{Guid.NewGuid():N}.png";
                    var path = Path.Combine(dir, fileName);
                    bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    return path;
                }
                finally
                {
                    GlobalUnlock(hMem);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            return null;
        }
    }
    public static Stream? CaptureScreenToPngStream()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        try
        {
            return CaptureScreenWindows();
        }
        catch
        {
            return null;
        }
    }

    public static (int X, int Y, int Width, int Height) GetVirtualScreenBounds()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return (0, 0, 1920, 1080);
        int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int w = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int h = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        return (x, y, w, h);
    }

    public static bool CaptureScreenAndSetToClipboardWindows(IntPtr windowHandle)
    {
        var (x, y, width, height) = GetVirtualScreenBounds();
        return CaptureRegionToClipboardWindows(windowHandle, x, y, width, height);
    }

    public static bool CaptureRegionToClipboardWindows(IntPtr windowHandle, int screenX, int screenY, int width, int height)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        if (width <= 0 || height <= 0)
            return false;

        try
        {
            using var bitmap = new System.Drawing.Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenX, screenY, 0, 0, new System.Drawing.Size(width, height));
            }

            return SetBitmapToClipboardWin32(windowHandle, bitmap);
        }
        catch
        {
            return false;
        }
    }

    private static bool SetBitmapToClipboardWin32(IntPtr hwnd, System.Drawing.Bitmap bitmap)
    {
        var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(bmpData.Stride);
            int size = stride * bitmap.Height;
            int headerSize = 40;
            int totalSize = headerSize + size;

            IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)totalSize);
            if (hMem == IntPtr.Zero) return false;

            try
            {
                IntPtr ptr = GlobalLock(hMem);
                if (ptr == IntPtr.Zero) return false;

                try
                {
                    var header = new byte[40];
                    int i = 0;
                    WriteInt32(header, i, 40); i += 4;
                    WriteInt32(header, i, bitmap.Width); i += 4;
                    WriteInt32(header, i, -bitmap.Height);
                    header[12] = 1;
                    header[14] = 32;
                    Marshal.Copy(header, 0, ptr, 40);

                    var row = new byte[stride];
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        IntPtr srcRow = IntPtr.Add(bmpData.Scan0, (bitmap.Height - 1 - y) * stride);
                        Marshal.Copy(srcRow, row, 0, stride);
                        Marshal.Copy(row, 0, IntPtr.Add(ptr, headerSize + y * stride), stride);
                    }
                }
                finally
                {
                    GlobalUnlock(hMem);
                }

                if (OpenClipboard(hwnd))
                {
                    try
                    {
                        EmptyClipboard();
                        if (SetClipboardData(CF_DIB, hMem) != IntPtr.Zero)
                        {
                            hMem = IntPtr.Zero;
                            return true;
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
            }
            finally
            {
                if (hMem != IntPtr.Zero)
                    GlobalFree(hMem);
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
        return false;
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static Stream? CaptureScreenWindows()
    {
        int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
            return null;

        using var bitmap = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
        }

        var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;
        return ms;
    }

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const uint CF_DIB = 8;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);
}
