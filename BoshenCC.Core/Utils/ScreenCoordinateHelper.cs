using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 屏幕坐标辅助工具类
    /// 提供多显示器环境下的坐标转换和屏幕信息查询功能
    /// </summary>
    public static class ScreenCoordinateHelper
    {
        #region 私有字段

        private static readonly object _lockObject = new object();
        private static List<ScreenInfo> _screens;
        private static DateTime _lastScreenUpdate = DateTime.MinValue;
        private static readonly TimeSpan _screenCacheTimeout = TimeSpan.FromSeconds(30);

        #endregion

        #region 公共方法

        /// <summary>
        /// 屏幕坐标转客户区坐标
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>客户区坐标点</returns>
        public static Point ScreenToClient(IntPtr windowHandle, Point screenPoint)
        {
            try
            {
                var winPoint = new WindowStructs.POINT(screenPoint.X, screenPoint.Y);
                if (Win32Api.ScreenToClient(windowHandle, ref winPoint))
                {
                    return new Point(winPoint.X, winPoint.Y);
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误，返回原始点
                System.Diagnostics.Debug.WriteLine($"屏幕坐标转客户区坐标失败: {ex.Message}");
            }

            return screenPoint;
        }

        /// <summary>
        /// 客户区坐标转屏幕坐标
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="clientPoint">客户区坐标点</param>
        /// <returns>屏幕坐标点</returns>
        public static Point ClientToScreen(IntPtr windowHandle, Point clientPoint)
        {
            try
            {
                var winPoint = new WindowStructs.POINT(clientPoint.X, clientPoint.Y);
                if (Win32Api.ClientToScreen(windowHandle, ref winPoint))
                {
                    return new Point(winPoint.X, winPoint.Y);
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误，返回原始点
                System.Diagnostics.Debug.WriteLine($"客户区坐标转屏幕坐标失败: {ex.Message}");
            }

            return clientPoint;
        }

        /// <summary>
        /// 矩形坐标转换：屏幕坐标转客户区坐标
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="screenRect">屏幕坐标矩形</param>
        /// <returns>客户区坐标矩形</returns>
        public static Rectangle ScreenRectToClientRect(IntPtr windowHandle, Rectangle screenRect)
        {
            var topLeft = ScreenToClient(windowHandle, screenRect.Location);
            var bottomRight = ScreenToClient(windowHandle, new Point(screenRect.Right, screenRect.Bottom));

            return Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }

        /// <summary>
        /// 矩形坐标转换：客户区坐标转屏幕坐标
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="clientRect">客户区坐标矩形</param>
        /// <returns>屏幕坐标矩形</returns>
        public static Rectangle ClientRectToScreenRect(IntPtr windowHandle, Rectangle clientRect)
        {
            var topLeft = ClientToScreen(windowHandle, clientRect.Location);
            var bottomRight = ClientToScreen(windowHandle, new Point(clientRect.Right, clientRect.Bottom));

            return Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }

        /// <summary>
        /// 获取所有显示器信息
        /// </summary>
        /// <returns>显示器信息列表</returns>
        public static List<ScreenInfo> GetAllScreens()
        {
            lock (_lockObject)
            {
                // 检查缓存是否过期
                if (_screens != null && DateTime.UtcNow - _lastScreenUpdate < _screenCacheTimeout)
                {
                    return new List<ScreenInfo>(_screens);
                }

                _screens = new List<ScreenInfo>();

                try
                {
                    // 使用Windows API枚举显示器
                    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

                    _lastScreenUpdate = DateTime.UtcNow;

                    System.Diagnostics.Debug.WriteLine($"已获取 {_screens.Count} 个显示器信息");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取显示器信息失败: {ex.Message}");
                    // 返回默认主显示器
                    _screens.Add(CreatePrimaryScreenInfo());
                }

                return new List<ScreenInfo>(_screens);
            }
        }

        /// <summary>
        /// 获取主显示器信息
        /// </summary>
        /// <returns>主显示器信息</returns>
        public static ScreenInfo GetPrimaryScreen()
        {
            var screens = GetAllScreens();
            return screens.FirstOrDefault(s => s.IsPrimary) ?? screens.FirstOrDefault() ?? CreatePrimaryScreenInfo();
        }

        /// <summary>
        /// 获取包含指定点的显示器
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>包含该点的显示器信息</returns>
        public static ScreenInfo GetScreenFromPoint(Point point)
        {
            var screens = GetAllScreens();
            return screens.FirstOrDefault(s => s.Bounds.Contains(point)) ?? GetPrimaryScreen();
        }

        /// <summary>
        /// 获取包含指定窗口的显示器
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>包含该窗口的显示器信息</returns>
        public static ScreenInfo GetScreenFromWindow(IntPtr windowHandle)
        {
            try
            {
                if (Win32Api.GetWindowRect(windowHandle, out WindowStructs.RECT rect))
                {
                    var windowCenter = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                    return GetScreenFromPoint(windowCenter);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取窗口所在显示器失败: {ex.Message}");
            }

            return GetPrimaryScreen();
        }

        /// <summary>
        /// 检查点是否在任何显示器边界内
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>是否在显示器边界内</returns>
        public static bool IsPointOnAnyScreen(Point point)
        {
            var screens = GetAllScreens();
            return screens.Any(s => s.Bounds.Contains(point));
        }

        /// <summary>
        /// 虚拟屏幕（所有显示器的合并区域）
        /// </summary>
        /// <returns>虚拟屏幕矩形</returns>
        public static Rectangle GetVirtualScreen()
        {
            try
            {
                // 使用系统API获取虚拟屏幕
                return new Rectangle(
                    System.Windows.Forms.SystemInformation.VirtualScreen.X,
                    System.Windows.Forms.SystemInformation.VirtualScreen.Y,
                    System.Windows.Forms.SystemInformation.VirtualScreen.Width,
                    System.Windows.Forms.SystemInformation.VirtualScreen.Height);
            }
            catch
            {
                // 如果失败，手动计算
                var screens = GetAllScreens();
                if (screens.Count == 0)
                    return Rectangle.Empty;

                int minX = screens.Min(s => s.Bounds.Left);
                int minY = screens.Min(s => s.Bounds.Top);
                int maxX = screens.Max(s => s.Bounds.Right);
                int maxY = screens.Max(s => s.Bounds.Bottom);

                return Rectangle.FromLTRB(minX, minY, maxX, maxY);
            }
        }

        /// <summary>
        /// 将点约束到最近的显示器边界内
        /// </summary>
        /// <param name="point">原始点</param>
        /// <returns>约束后的点</returns>
        public static Point ConstrainPointToScreen(Point point)
        {
            var screen = GetScreenFromPoint(point);
            var bounds = screen.Bounds;

            int x = Math.Max(bounds.Left, Math.Min(bounds.Right - 1, point.X));
            int y = Math.Max(bounds.Top, Math.Min(bounds.Bottom - 1, point.Y));

            return new Point(x, y);
        }

        /// <summary>
        /// 将矩形约束到最近的显示器边界内
        /// </summary>
        /// <param name="rect">原始矩形</param>
        /// <returns>约束后的矩形</returns>
        public static Rectangle ConstrainRectToScreen(Rectangle rect)
        {
            var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            var screen = GetScreenFromPoint(center);
            var bounds = screen.Bounds;

            // 确保矩形不超出屏幕边界
            int x = rect.X;
            int y = rect.Y;
            int width = rect.Width;
            int height = rect.Height;

            if (rect.Right > bounds.Right)
                x = bounds.Right - width;
            if (rect.Bottom > bounds.Bottom)
                y = bounds.Bottom - height;
            if (rect.Left < bounds.Left)
                x = bounds.Left;
            if (rect.Top < bounds.Top)
                y = bounds.Top;

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 计算两个点之间的距离
        /// </summary>
        /// <param name="point1">第一个点</param>
        /// <param name="point2">第二个点</param>
        /// <returns>距离</returns>
        public static double GetDistance(Point point1, Point point2)
        {
            int dx = point2.X - point1.X;
            int dy = point2.Y - point1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 强制刷新显示器信息缓存
        /// </summary>
        public static void RefreshScreenCache()
        {
            lock (_lockObject)
            {
                _lastScreenUpdate = DateTime.MinValue;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 显示器枚举回调函数
        /// </summary>
        private static bool MonitorEnumProc(IntPtr monitor, IntPtr hdcMonitor, ref Rectangle lprcMonitor, IntPtr dwData)
        {
            try
            {
                var monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));

                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var screenInfo = new ScreenInfo
                    {
                        Bounds = new Rectangle(
                            monitorInfo.rcMonitor.Left,
                            monitorInfo.rcMonitor.Top,
                            monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                            monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top),
                        WorkingArea = new Rectangle(
                            monitorInfo.rcWork.Left,
                            monitorInfo.rcWork.Top,
                            monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
                            monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top),
                        IsPrimary = (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0,
                        DeviceName = monitorInfo.szDevice,
                        Handle = monitor
                    };

                    _screens.Add(screenInfo);
                }

                return true; // 继续枚举
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示器枚举回调失败: {ex.Message}");
                return true; // 继续枚举其他显示器
            }
        }

        /// <summary>
        /// 创建默认主显示器信息
        /// </summary>
        /// <returns>主显示器信息</returns>
        private static ScreenInfo CreatePrimaryScreenInfo()
        {
            try
            {
                var primaryBounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                var primaryWorkingArea = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1040);

                return new ScreenInfo
                {
                    Bounds = primaryBounds,
                    WorkingArea = primaryWorkingArea,
                    IsPrimary = true,
                    DeviceName = "\\\\.\\DISPLAY1",
                    Handle = IntPtr.Zero
                };
            }
            catch
            {
                // 完全失败时的默认值
                return new ScreenInfo
                {
                    Bounds = new Rectangle(0, 0, 1920, 1080),
                    WorkingArea = new Rectangle(0, 0, 1920, 1040),
                    IsPrimary = true,
                    DeviceName = "\\\\.\\DISPLAY1",
                    Handle = IntPtr.Zero
                };
            }
        }

        #endregion

        #region Windows API 声明

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdcMonitor, ref Rectangle lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public uint cbSize;
            public Rectangle rcMonitor;
            public Rectangle rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        private const uint MONITORINFOF_PRIMARY = 0x00000001;

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 屏幕信息
    /// </summary>
    public class ScreenInfo
    {
        /// <summary>
        /// 屏幕边界
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// 工作区域（排除任务栏）
        /// </summary>
        public Rectangle WorkingArea { get; set; }

        /// <summary>
        /// 是否为主显示器
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 显示器句柄
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// 屏幕宽度
        /// </summary>
        public int Width => Bounds.Width;

        /// <summary>
        /// 屏幕高度
        /// </summary>
        public int Height => Bounds.Height;

        /// <summary>
        /// DPI缩放比例（默认为1.0）
        /// </summary>
        public float DpiScaleX => 1.0f; // TODO: 实现DPI感知

        /// <summary>
        /// DPI缩放比例（默认为1.0）
        /// </summary>
        public float DpiScaleY => 1.0f; // TODO: 实现DPI感知

        public override string ToString()
        {
            return $"Screen: {DeviceName} ({Bounds.Width}x{Bounds.Height}) {(IsPrimary ? "[Primary]" : "")}";
        }
    }

    #endregion
}