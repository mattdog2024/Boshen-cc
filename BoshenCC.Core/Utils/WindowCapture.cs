using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 窗口捕获工具类，支持窗口检测和截图功能
    /// </summary>
    public class WindowCapture
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool GetWindowInfo(IntPtr hWnd, out WINDOWINFO pwi);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLongA(IntPtr hWnd, int nIndex);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
                                         IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;
        }

        private const int SRCCOPY = 0x00CC0020;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        /// <summary>
        /// 窗口信息
        /// </summary>
        public class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
            public Rectangle Bounds { get; set; }
            public bool IsVisible { get; set; }
            public bool IsMinimized { get; set; }
            public string ProcessName { get; set; }
        }

        /// <summary>
        /// 获取所有可见窗口
        /// </summary>
        /// <returns>可见窗口列表</returns>
        public static List<WindowInfo> GetVisibleWindows()
        {
            var windows = new List<WindowInfo>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindow(hWnd) && IsWindowVisible(hWnd))
                {
                    var windowInfo = GetWindowInfoByHandle(hWnd);
                    if (windowInfo != null && !string.IsNullOrEmpty(windowInfo.Title))
                    {
                        // 过滤掉工具窗口
                        long exStyle = GetWindowLongA(hWnd, GWL_EXSTYLE);
                        if ((exStyle & WS_EX_TOOLWINDOW) == 0)
                        {
                            windows.Add(windowInfo);
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows.OrderBy(w => w.Title).ToList();
        }

        /// <summary>
        /// 根据句柄获取窗口信息
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>窗口信息</returns>
        public static WindowInfo GetWindowInfoByHandle(IntPtr hWnd)
        {
            try
            {
                if (!IsWindow(hWnd))
                    return null;

                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return null;

                var title = new StringBuilder(length + 1);
                GetWindowText(hWnd, title, title.Capacity);

                if (!GetWindowRect(hWnd, out RECT rect))
                    return null;

                return new WindowInfo
                {
                    Handle = hWnd,
                    Title = title.ToString(),
                    Bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    IsVisible = IsWindowVisible(hWnd),
                    IsMinimized = IsIconic(hWnd)
                };
            }
            catch (Exception ex)
            {
                // 静默处理异常，某些窗口可能会抛出异常
                return null;
            }
        }

        /// <summary>
        /// 查找指定标题的窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="exactMatch">是否精确匹配</param>
        /// <returns>窗口信息</returns>
        public static WindowInfo FindWindowByTitle(string title, bool exactMatch = false)
        {
            var windows = GetVisibleWindows();

            if (exactMatch)
            {
                return windows.FirstOrDefault(w => w.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return windows.FirstOrDefault(w => w.Title.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        /// <summary>
        /// 查找文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口信息</returns>
        public static WindowInfo FindWenhuaWindow()
        {
            // 文华行情常见的窗口标题关键词
            var winhuaKeywords = new[]
            {
                "文华财经",
                "文华行情",
                "赢顺WH6",
                "文华赢顺",
                "Wenhua",
                "WH6"
            };

            var windows = GetVisibleWindows();

            foreach (var keyword in winhuaKeywords)
            {
                var window = windows.FirstOrDefault(w => w.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                if (window != null)
                    return window;
            }

            return null;
        }

        /// <summary>
        /// 获取当前活动窗口
        /// </summary>
        /// <returns>当前活动窗口信息</returns>
        public static WindowInfo GetActiveWindow()
        {
            IntPtr hWnd = GetForegroundWindow();
            return GetWindowInfoByHandle(hWnd);
        }

        /// <summary>
        /// 检查窗口是否存在
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否存在</returns>
        public static bool IsWindowExists(IntPtr hWnd)
        {
            return IsWindow(hWnd);
        }

        /// <summary>
        /// 检查窗口是否可见
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否可见</returns>
        public static bool IsWindowVisible(IntPtr hWnd)
        {
            return IsWindow(hWnd) && IsWindowVisible(hWnd);
        }

        /// <summary>
        /// 检查窗口是否最小化
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否最小化</returns>
        public static bool IsWindowMinimized(IntPtr hWnd)
        {
            return IsIconic(hWnd);
        }

        /// <summary>
        /// 截取指定窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>窗口截图</returns>
        public static Bitmap CaptureWindow(IntPtr hWnd)
        {
            try
            {
                if (!IsWindow(hWnd))
                    throw new ArgumentException("无效的窗口句柄");

                if (!GetWindowRect(hWnd, out RECT rect))
                    throw new InvalidOperationException("无法获取窗口尺寸");

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                    throw new InvalidOperationException("窗口尺寸无效");

                var hdcSrc = GetWindowDC(hWnd);
                var hdcDest = CreateCompatibleDC(hdcSrc);
                var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                var hOld = SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);

                var bitmap = Bitmap.FromHbitmap(hBitmap);

                SelectObject(hdcDest, hOld);
                DeleteObject(hBitmap);
                DeleteObject(hdcDest);
                ReleaseDC(hWnd, hdcSrc);

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"窗口截图失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步截取指定窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>窗口截图任务</returns>
        public static async Task<Bitmap> CaptureWindowAsync(IntPtr hWnd)
        {
            return await Task.Run(() => CaptureWindow(hWnd));
        }

        /// <summary>
        /// 等待窗口出现
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="exactMatch">是否精确匹配</param>
        /// <returns>窗口信息</returns>
        public static async Task<WindowInfo> WaitForWindow(string title, int timeoutMs = 5000, bool exactMatch = false)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                var window = FindWindowByTitle(title, exactMatch);
                if (window != null)
                    return window;

                await Task.Delay(100);
            }

            return null;
        }

        /// <summary>
        /// 等待文华行情窗口出现
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>文华行情窗口信息</returns>
        public static async Task<WindowInfo> WaitForWenhuaWindow(int timeoutMs = 10000)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                var window = FindWenhuaWindow();
                if (window != null)
                    return window;

                await Task.Delay(200);
            }

            return null;
        }

        /// <summary>
        /// 获取窗口的客户端区域截图
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>客户端区域截图</returns>
        public static Bitmap CaptureWindowClient(IntPtr hWnd)
        {
            try
            {
                if (!GetWindowInfo(hWnd, out WINDOWINFO wi))
                    throw new InvalidOperationException("无法获取窗口信息");

                int width = wi.rcClient.Right - wi.rcClient.Left;
                int height = wi.rcClient.Bottom - wi.rcClient.Top;

                if (width <= 0 || height <= 0)
                    throw new InvalidOperationException("窗口客户端尺寸无效");

                var hdcSrc = GetWindowDC(hWnd);
                var hdcDest = CreateCompatibleDC(hdcSrc);
                var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
                var hOld = SelectObject(hdcDest, hBitmap);

                // 计算客户端区域的偏移
                int clientX = wi.rcClient.Left - wi.rcWindow.Left;
                int clientY = wi.rcClient.Top - wi.rcWindow.Top;

                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, clientX, clientY, SRCCOPY);

                var bitmap = Bitmap.FromHbitmap(hBitmap);

                SelectObject(hdcDest, hOld);
                DeleteObject(hBitmap);
                DeleteObject(hdcDest);
                ReleaseDC(hWnd, hdcSrc);

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"窗口客户端截图失败: {ex.Message}", ex);
            }
        }
    }
}