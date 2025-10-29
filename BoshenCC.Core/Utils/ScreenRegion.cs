using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 屏幕区域工具类，支持区域定义和截图功能
    /// </summary>
    public class ScreenRegion
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

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

        private const int SRCCOPY = 0x00CC0020;

        /// <summary>
        /// 屏幕区域定义
        /// </summary>
        public class Region
        {
            public string Name { get; set; }
            public Rectangle Rectangle { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public Dictionary<string, object> Properties { get; set; }

            public Region()
            {
                CreatedAt = DateTime.Now;
                Properties = new Dictionary<string, object>();
            }

            public Region(string name, Rectangle rectangle, string description = null) : this()
            {
                Name = name;
                Rectangle = rectangle;
                Description = description;
            }

            /// <summary>
            /// 检查区域是否有效
            /// </summary>
            public bool IsValid => Rectangle.Width > 0 && Rectangle.Height > 0;

            /// <summary>
            /// 获取区域中心点
            /// </summary>
            public Point Center => new Point(
                Rectangle.X + Rectangle.Width / 2,
                Rectangle.Y + Rectangle.Height / 2);

            /// <summary>
            /// 检查点是否在区域内
            /// </summary>
            public bool Contains(Point point) => Rectangle.Contains(point);

            /// <summary>
            /// 检查区域是否与另一个区域相交
            /// </summary>
            public bool IntersectsWith(Region other) => Rectangle.IntersectsWith(other.Rectangle);

            /// <summary>
            /// 获取与另一个区域的交集
            /// </summary>
            public Region GetIntersection(Region other)
            {
                var intersection = Rectangle.Intersect(other.Rectangle);
                return new Region($"{Name}_∩_{other.Name}", intersection, "交集区域");
            }
        }

        /// <summary>
        /// 预定义的常用区域
        /// </summary>
        public static class CommonRegions
        {
            private static Rectangle _screenBounds = Screen.PrimaryScreen.Bounds;

            /// <summary>
            /// 左上角区域 (1/4屏幕)
            /// </summary>
            public static Region TopLeft => new Region("TopLeft",
                new Rectangle(0, 0, _screenBounds.Width / 2, _screenBounds.Height / 2),
                "屏幕左上角区域");

            /// <summary>
            /// 右上角区域 (1/4屏幕)
            /// </summary>
            public static Region TopRight => new Region("TopRight",
                new Rectangle(_screenBounds.Width / 2, 0, _screenBounds.Width / 2, _screenBounds.Height / 2),
                "屏幕右上角区域");

            /// <summary>
            /// 左下角区域 (1/4屏幕)
            /// </summary>
            public static Region BottomLeft => new Region("BottomLeft",
                new Rectangle(0, _screenBounds.Height / 2, _screenBounds.Width / 2, _screenBounds.Height / 2),
                "屏幕左下角区域");

            /// <summary>
            /// 右下角区域 (1/4屏幕)
            /// </summary>
            public static Region BottomRight => new Region("BottomRight",
                new Rectangle(_screenBounds.Width / 2, _screenBounds.Height / 2, _screenBounds.Width / 2, _screenBounds.Height / 2),
                "屏幕右下角区域");

            /// <summary>
            /// 上半部分区域
            /// </summary>
            public static Region TopHalf => new Region("TopHalf",
                new Rectangle(0, 0, _screenBounds.Width, _screenBounds.Height / 2),
                "屏幕上半部分");

            /// <summary>
            /// 下半部分区域
            /// </summary>
            public static Region BottomHalf => new Region("BottomHalf",
                new Rectangle(0, _screenBounds.Height / 2, _screenBounds.Width, _screenBounds.Height / 2),
                "屏幕下半部分");

            /// <summary>
            /// 左半部分区域
            /// </summary>
            public static Region LeftHalf => new Region("LeftHalf",
                new Rectangle(0, 0, _screenBounds.Width / 2, _screenBounds.Height),
                "屏幕左半部分");

            /// <summary>
            /// 右半部分区域
            /// </summary>
            public static Region RightHalf => new Region("RightHalf",
                new Rectangle(_screenBounds.Width / 2, 0, _screenBounds.Width / 2, _screenBounds.Height),
                "屏幕右半部分");

            /// <summary>
            /// 中心区域 (屏幕中央1/4)
            /// </summary>
            public static Region Center => new Region("Center",
                new Rectangle(_screenBounds.Width / 4, _screenBounds.Height / 4,
                             _screenBounds.Width / 2, _screenBounds.Height / 2),
                "屏幕中心区域");
        }

        /// <summary>
        /// 截取指定区域
        /// </summary>
        /// <param name="region">区域定义</param>
        /// <returns>区域截图</returns>
        public static Bitmap CaptureRegion(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            if (!region.IsValid)
                throw new ArgumentException("区域尺寸无效", nameof(region));

            return CaptureRegion(region.Rectangle);
        }

        /// <summary>
        /// 截取指定矩形区域
        /// </summary>
        /// <param name="rectangle">矩形区域</param>
        /// <returns>区域截图</returns>
        public static Bitmap CaptureRegion(Rectangle rectangle)
        {
            try
            {
                // 确保区域在屏幕范围内
                var screenBounds = Screen.PrimaryScreen.Bounds;
                var captureRect = Rectangle.Intersect(rectangle, screenBounds);

                if (captureRect.IsEmpty)
                    throw new ArgumentException("截图区域超出屏幕范围", nameof(rectangle));

                var hdcSrc = GetWindowDC(GetDesktopWindow());
                var hdcDest = CreateCompatibleDC(hdcSrc);
                var hBitmap = CreateCompatibleBitmap(hdcSrc, captureRect.Width, captureRect.Height);
                var hOld = SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, captureRect.Width, captureRect.Height,
                       hdcSrc, captureRect.X, captureRect.Y, SRCCOPY);

                var bitmap = Bitmap.FromHbitmap(hBitmap);

                SelectObject(hdcDest, hOld);
                DeleteObject(hBitmap);
                DeleteObject(hdcDest);
                ReleaseDC(GetDesktopWindow(), hdcSrc);

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"区域截图失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步截取指定区域
        /// </summary>
        /// <param name="region">区域定义</param>
        /// <returns>区域截图任务</returns>
        public static async Task<Bitmap> CaptureRegionAsync(Region region)
        {
            return await Task.Run(() => CaptureRegion(region));
        }

        /// <summary>
        /// 异步截取指定矩形区域
        /// </summary>
        /// <param name="rectangle">矩形区域</param>
        /// <returns>区域截图任务</returns>
        public static async Task<Bitmap> CaptureRegionAsync(Rectangle rectangle)
        {
            return await Task.Run(() => CaptureRegion(rectangle));
        }

        /// <summary>
        /// 批量截取多个区域
        /// </summary>
        /// <param name="regions">区域列表</param>
        /// <returns>截图字典（区域名称 -> 截图）</returns>
        public static Dictionary<string, Bitmap> CaptureMultipleRegions(IEnumerable<Region> regions)
        {
            var result = new Dictionary<string, Bitmap>();

            foreach (var region in regions)
            {
                try
                {
                    var screenshot = CaptureRegion(region);
                    result[region.Name] = screenshot;
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他区域
                    Console.WriteLine($"截取区域 {region.Name} 失败: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 异步批量截取多个区域
        /// </summary>
        /// <param name="regions">区域列表</param>
        /// <returns>截图字典任务</returns>
        public static async Task<Dictionary<string, Bitmap>> CaptureMultipleRegionsAsync(IEnumerable<Region> regions)
        {
            return await Task.Run(() => CaptureMultipleRegions(regions));
        }

        /// <summary>
        /// 创建自定义区域
        /// </summary>
        /// <param name="name">区域名称</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="description">描述</param>
        /// <returns>区域定义</returns>
        public static Region CreateRegion(string name, int x, int y, int width, int height, string description = null)
        {
            return new Region(name, new Rectangle(x, y, width, height), description);
        }

        /// <summary>
        /// 根据比例创建区域
        /// </summary>
        /// <param name="name">区域名称</param>
        /// <param name="xRatio">X坐标比例 (0-1)</param>
        /// <param name="yRatio">Y坐标比例 (0-1)</param>
        /// <param name="widthRatio">宽度比例 (0-1)</param>
        /// <param name="heightRatio">高度比例 (0-1)</param>
        /// <param name="description">描述</param>
        /// <returns>区域定义</returns>
        public static Region CreateRegionByRatio(string name, double xRatio, double yRatio,
                                                double widthRatio, double heightRatio, string description = null)
        {
            var screenBounds = Screen.PrimaryScreen.Bounds;

            if (xRatio < 0 || xRatio > 1 || yRatio < 0 || yRatio > 1 ||
                widthRatio <= 0 || widthRatio > 1 || heightRatio <= 0 || heightRatio > 1)
                throw new ArgumentException("比例参数必须在0-1范围内");

            int x = (int)(screenBounds.Width * xRatio);
            int y = (int)(screenBounds.Height * yRatio);
            int width = (int)(screenBounds.Width * widthRatio);
            int height = (int)(screenBounds.Height * heightRatio);

            return new Region(name, new Rectangle(x, y, width, height), description);
        }

        /// <summary>
        /// 获取屏幕边界
        /// </summary>
        /// <returns>屏幕边界矩形</returns>
        public static Rectangle GetScreenBounds()
        {
            return Screen.PrimaryScreen.Bounds;
        }

        /// <summary>
        /// 获取所有屏幕边界
        /// </summary>
        /// <returns>所有屏幕的边界矩形</returns>
        public static Rectangle[] GetAllScreenBounds()
        {
            return Screen.AllScreens.Select(screen => screen.Bounds).ToArray();
        }

        /// <summary>
        /// 检查区域是否在屏幕范围内
        /// </summary>
        /// <param name="region">区域</param>
        /// <param name="screenIndex">屏幕索引（-1表示任意屏幕）</param>
        /// <returns>是否在屏幕范围内</returns>
        public static bool IsRegionOnScreen(Region region, int screenIndex = -1)
        {
            if (region == null || !region.IsValid)
                return false;

            if (screenIndex >= 0 && screenIndex < Screen.AllScreens.Length)
            {
                var screenBounds = Screen.AllScreens[screenIndex].Bounds;
                return screenBounds.Contains(region.Rectangle);
            }
            else
            {
                return Screen.AllScreens.Any(screen => screen.Bounds.Contains(region.Rectangle));
            }
        }

        /// <summary>
        /// 调整区域以适应屏幕边界
        /// </summary>
        /// <param name="region">原始区域</param>
        /// <returns>调整后的区域</returns>
        public static Region AdjustRegionToScreen(Region region)
        {
            if (region == null)
                return null;

            var screenBounds = Screen.PrimaryScreen.Bounds;
            var adjustedRect = Rectangle.Intersect(region.Rectangle, screenBounds);

            return new Region(region.Name, adjustedRect, region.Description);
        }

        /// <summary>
        /// 保存区域到文件
        /// </summary>
        /// <param name="region">区域定义</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">图片格式</param>
        public static async Task SaveRegionToFile(Region region, string filePath, ImageFormat format)
        {
            var screenshot = await CaptureRegionAsync(region);
            try
            {
                screenshot.Save(filePath, format);
            }
            finally
            {
                screenshot.Dispose();
            }
        }

        /// <summary>
        /// 连续监控区域变化
        /// </summary>
        /// <param name="region">监控区域</param>
        /// <param name="intervalMs">监控间隔（毫秒）</param>
        /// <param name="callback">变化回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>监控任务</returns>
        public static async Task MonitorRegionChanges(Region region, int intervalMs,
            Func<Bitmap, Task> callback, System.Threading.CancellationToken cancellationToken = default)
        {
            Bitmap previousImage = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var currentImage = await CaptureRegionAsync(region);

                    if (previousImage != null && !ImagesEqual(previousImage, currentImage))
                    {
                        await callback(currentImage);
                    }

                    previousImage?.Dispose();
                    previousImage = currentImage;

                    await Task.Delay(intervalMs, cancellationToken);
                }
            }
            finally
            {
                previousImage?.Dispose();
            }
        }

        /// <summary>
        /// 比较两张图片是否相同
        /// </summary>
        private static bool ImagesEqual(Bitmap img1, Bitmap img2)
        {
            if (img1 == null || img2 == null)
                return false;

            if (img1.Width != img2.Width || img1.Height != img2.Height)
                return false;

            for (int y = 0; y < img1.Height; y++)
            {
                for (int x = 0; x < img1.Width; x++)
                {
                    if (img1.GetPixel(x, y) != img2.GetPixel(x, y))
                        return false;
                }
            }

            return true;
        }
    }
}