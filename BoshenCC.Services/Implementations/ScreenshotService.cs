using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BoshenCC.Services.Interfaces;
using BoshenCC.Core.Utils;

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

        #region 异步截图方法

        /// <summary>
        /// 异步截取全屏
        /// </summary>
        /// <returns>屏幕截图任务</returns>
        public async Task<Bitmap> CaptureFullScreenAsync()
        {
            return await Task.Run(() => CaptureFullScreen());
        }

        /// <summary>
        /// 异步截取指定区域
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>区域截图任务</returns>
        public async Task<Bitmap> CaptureRegionAsync(int x, int y, int width, int height)
        {
            return await Task.Run(() => CaptureRegion(x, y, width, height));
        }

        /// <summary>
        /// 异步截取指定窗口
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>窗口截图任务</returns>
        public async Task<Bitmap> CaptureWindowAsync(IntPtr handle)
        {
            return await WindowCapture.CaptureWindowAsync(handle);
        }

        /// <summary>
        /// 异步保存截图到文件
        /// </summary>
        /// <param name="screenshot">截图</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">图片格式</param>
        /// <returns>保存任务</returns>
        public async Task SaveToFileAsync(Bitmap screenshot, string filePath, ImageFormat format)
        {
            await Task.Run(() => SaveToFile(screenshot, filePath, format));
        }

        #endregion

        #region 窗口检测方法

        /// <summary>
        /// 获取所有可见窗口
        /// </summary>
        /// <returns>可见窗口列表</returns>
        public List<WindowCapture.WindowInfo> GetVisibleWindows()
        {
            return WindowCapture.GetVisibleWindows();
        }

        /// <summary>
        /// 查找指定标题的窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="exactMatch">是否精确匹配</param>
        /// <returns>窗口信息</returns>
        public WindowCapture.WindowInfo FindWindowByTitle(string title, bool exactMatch = false)
        {
            return WindowCapture.FindWindowByTitle(title, exactMatch);
        }

        /// <summary>
        /// 查找文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口信息</returns>
        public WindowCapture.WindowInfo FindWenhuaWindow()
        {
            return WindowCapture.FindWenhuaWindow();
        }

        /// <summary>
        /// 获取当前活动窗口
        /// </summary>
        /// <returns>当前活动窗口信息</returns>
        public WindowCapture.WindowInfo GetActiveWindow()
        {
            return WindowCapture.GetActiveWindow();
        }

        /// <summary>
        /// 等待文华行情窗口出现
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>文华行情窗口信息</returns>
        public async Task<WindowCapture.WindowInfo> WaitForWenhuaWindowAsync(int timeoutMs = 10000)
        {
            return await WindowCapture.WaitForWenhuaWindow(timeoutMs);
        }

        /// <summary>
        /// 截取文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口截图</returns>
        public Bitmap CaptureWenhuaWindow()
        {
            var window = FindWenhuaWindow();
            if (window == null)
                throw new InvalidOperationException("未找到文华行情窗口");

            return WindowCapture.CaptureWindow(window.Handle);
        }

        /// <summary>
        /// 异步截取文华行情软件窗口
        /// </summary>
        /// <returns>文华行情窗口截图任务</returns>
        public async Task<Bitmap> CaptureWenhuaWindowAsync()
        {
            var window = await WaitForWenhuaWindowAsync();
            if (window == null)
                throw new InvalidOperationException("未找到文华行情窗口");

            return await WindowCapture.CaptureWindowAsync(window.Handle);
        }

        #endregion

        #region 高级截图方法

        /// <summary>
        /// 截取指定窗口的客户端区域
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>客户端区域截图</returns>
        public Bitmap CaptureWindowClient(IntPtr handle)
        {
            return WindowCapture.CaptureWindowClient(handle);
        }

        /// <summary>
        /// 异步截取指定窗口的客户端区域
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>客户端区域截图任务</returns>
        public async Task<Bitmap> CaptureWindowClientAsync(IntPtr handle)
        {
            return await Task.Run(() => CaptureWindowClient(handle));
        }

        /// <summary>
        /// 批量截取多个窗口
        /// </summary>
        /// <param name="handles">窗口句柄列表</param>
        /// <returns>截图字典（句柄 -> 截图）</returns>
        public Dictionary<IntPtr, Bitmap> CaptureMultipleWindows(List<IntPtr> handles)
        {
            var result = new Dictionary<IntPtr, Bitmap>();

            foreach (var handle in handles)
            {
                try
                {
                    var screenshot = WindowCapture.CaptureWindow(handle);
                    result[handle] = screenshot;
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他窗口
                    Console.WriteLine($"截取窗口 {handle} 失败: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 异步批量截取多个窗口
        /// </summary>
        /// <param name="handles">窗口句柄列表</param>
        /// <returns>截图字典任务</returns>
        public async Task<Dictionary<IntPtr, Bitmap>> CaptureMultipleWindowsAsync(List<IntPtr> handles)
        {
            return await Task.Run(() => CaptureMultipleWindows(handles));
        }

        /// <summary>
        /// 截取屏幕区域（使用ScreenRegion工具）
        /// </summary>
        /// <param name="region">屏幕区域</param>
        /// <returns>区域截图</returns>
        public Bitmap CaptureScreenRegion(ScreenRegion.Region region)
        {
            return ScreenRegion.CaptureRegion(region);
        }

        /// <summary>
        /// 异步截取屏幕区域
        /// </summary>
        /// <param name="region">屏幕区域</param>
        /// <returns>区域截图任务</returns>
        public async Task<Bitmap> CaptureScreenRegionAsync(ScreenRegion.Region region)
        {
            return await ScreenRegion.CaptureRegionAsync(region);
        }

        /// <summary>
        /// 连续截图监控
        /// </summary>
        /// <param name="region">监控区域</param>
        /// <param name="intervalMs">截图间隔（毫秒）</param>
        /// <param name="callback">截图回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>监控任务</returns>
        public async Task StartContinuousCapture(ScreenRegion.Region region, int intervalMs,
            Func<Bitmap, Task> callback, System.Threading.CancellationToken cancellationToken = default)
        {
            await ScreenRegion.MonitorRegionChanges(region, intervalMs, callback, cancellationToken);
        }

        #endregion

        #region 性能优化方法

        /// <summary>
        /// 高性能截图（优化版本）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>区域截图</returns>
        public Bitmap CaptureRegionFast(int x, int y, int width, int height)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 确保尺寸在合理范围内
                width = Math.Max(1, Math.Min(width, 4096));
                height = Math.Max(1, Math.Min(height, 4096));

                var bitmap = CaptureRegion(x, y, width, height);

                stopwatch.Stop();
                Console.WriteLine($"快速截图完成，耗时: {stopwatch.ElapsedMilliseconds}ms");

                return bitmap;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                throw new InvalidOperationException($"快速截图失败 (耗时: {stopwatch.ElapsedMilliseconds}ms): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量截图并缓存
        /// </summary>
        /// <param name="regions">区域列表</param>
        /// <param name="cacheTimeoutMs">缓存超时时间（毫秒）</param>
        /// <returns>缓存管理器</returns>
        public ScreenshotCache CreateScreenshotCache(List<ScreenRegion.Region> regions, int cacheTimeoutMs = 1000)
        {
            return new ScreenshotCache(regions, cacheTimeoutMs, this);
        }

        #endregion

        #region DPI处理

        /// <summary>
        /// 获取DPI缩放比例
        /// </summary>
        /// <returns>DPI缩放比例</returns>
        public double GetDpiScale()
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiX / 96.0;
            }
        }

        /// <summary>
        /// 根据DPI调整坐标
        /// </summary>
        /// <param name="x">原始X坐标</param>
        /// <param name="y">原始Y坐标</param>
        /// <param name="width">原始宽度</param>
        /// <param name="height">原始高度</param>
        /// <returns>调整后的矩形</returns>
        public Rectangle AdjustForDpi(int x, int y, int width, int height)
        {
            var scale = GetDpiScale();
            return new Rectangle(
                (int)(x / scale),
                (int)(y / scale),
                (int)(width / scale),
                (int)(height / scale)
            );
        }

        #endregion
    }

    /// <summary>
    /// 截图缓存管理器
    /// </summary>
    public class ScreenshotCache
    {
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly List<ScreenRegion.Region> _regions;
        private readonly int _cacheTimeoutMs;
        private readonly IScreenshotService _screenshotService;
        private readonly object _lock = new object();

        private class CacheEntry
        {
            public Bitmap Bitmap { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public ScreenshotCache(List<ScreenRegion.Region> regions, int cacheTimeoutMs, IScreenshotService screenshotService)
        {
            _regions = regions ?? new List<ScreenRegion.Region>();
            _cacheTimeoutMs = cacheTimeoutMs;
            _screenshotService = screenshotService;
            _cache = new Dictionary<string, CacheEntry>();
        }

        /// <summary>
        /// 获取区域截图（使用缓存）
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>截图</returns>
        public Bitmap GetScreenshot(string regionName)
        {
            lock (_lock)
            {
                // 清理过期缓存
                CleanupExpiredCache();

                if (_cache.TryGetValue(regionName, out var entry))
                {
                    return entry.Bitmap;
                }

                // 查找区域并截图
                var region = _regions.FirstOrDefault(r => r.Name == regionName);
                if (region != null)
                {
                    var bitmap = _screenshotService.CaptureScreenRegion(region);
                    _cache[regionName] = new CacheEntry
                    {
                        Bitmap = bitmap,
                        CreatedAt = DateTime.Now
                    };
                    return bitmap;
                }

                return null;
            }
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        private void CleanupExpiredCache()
        {
            var now = DateTime.Now;
            var expiredKeys = _cache.Where(kvp =>
                (now - kvp.Value.CreatedAt).TotalMilliseconds > _cacheTimeoutMs)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                if (_cache[key].Bitmap != null)
                {
                    _cache[key].Bitmap.Dispose();
                }
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                foreach (var entry in _cache.Values)
                {
                    entry.Bitmap?.Dispose();
                }
                _cache.Clear();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearCache();
        }
    }

    // 避免命名冲突的GDI类
    internal static class GDI
    {
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    }
}