using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Services.Implementations
{
    /// <summary>
    /// 截图服务实现
    /// </summary>
    public class ScreenshotService : IScreenshotService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int SRCCOPY = 0x00CC0020;

        public Bitmap CaptureFullScreen()
        {
            var bounds = GetScreenBounds();
            return CaptureRegion(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public Bitmap CaptureRegion(int x, int y, int width, int height)
        {
            try
            {
                var hdcSrc = GetWindowDC(GetDesktopWindow());
                var hdcDest = CreateCompatibleDC(hdcSrc);
                var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                var hOld = GDI.SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, x, y, SRCCOPY);

                var bitmap = Bitmap.FromHbitmap(hBitmap);

                GDI.SelectObject(hdcDest, hOld); // restore
                DeleteObject(hBitmap);
                DeleteObject(hdcDest);
                ReleaseDC(GetDesktopWindow(), hdcSrc);

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"截图失败: {ex.Message}", ex);
            }
        }

        public Bitmap CaptureWindow(IntPtr handle)
        {
            if (!GetWindowRect(handle, out RECT rect))
            {
                throw new ArgumentException("无效的窗口句柄");
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            return CaptureRegion(rect.Left, rect.Top, width, height);
        }

        public Rectangle GetScreenBounds()
        {
            return Screen.PrimaryScreen.Bounds;
        }

        public void SaveToFile(Bitmap screenshot, string filePath, ImageFormat format)
        {
            if (screenshot == null)
                throw new ArgumentNullException(nameof(screenshot));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                screenshot.Save(filePath, format);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存截图失败: {ex.Message}", ex);
            }
        }
    }

    // 避免命名冲突的GDI类
    internal static class GDI
    {
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    }
}