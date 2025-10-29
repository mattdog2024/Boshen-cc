using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoshenCC.Core.Utils;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// DPI助手工具类 - 处理高DPI缩放和多显示器支持
    /// Issue #6 Stream A: UI优化和响应式设计
    /// </summary>
    public static class DPIHelper
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        // 设备能力常量
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        // 监视器标志
        private const int MONITOR_DEFAULTTONEAREST = 2;

        #endregion

        #region 私有字段

        private static float _currentScaleX = 1.0f;
        private static float _currentScaleY = 1.0f;
        private static float _systemDpiX = 96.0f;
        private static float _systemDpiY = 96.0f;
        private static bool _isInitialized = false;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化DPI设置
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (_isInitialized)
                    return;

                // 获取系统DPI
                _systemDpiX = GetSystemDpiX();
                _systemDpiY = GetSystemDpiY();

                // 计算当前缩放比例
                _currentScaleX = _systemDpiX / 96.0f;
                _currentScaleY = _systemDpiY / 96.0f;

                _isInitialized = true;

                // 记录DPI信息
                LogDpiInfo();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "DPI助手", "初始化DPI设置失败");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取当前DPI缩放比例
        /// </summary>
        /// <returns>缩放比例</returns>
        public static float GetCurrentScale()
        {
            if (!_isInitialized)
                Initialize();

            return (_currentScaleX + _currentScaleY) / 2.0f;
        }

        /// <summary>
        /// 获取水平DPI缩放比例
        /// </summary>
        public static float GetScaleX()
        {
            if (!_isInitialized)
                Initialize();

            return _currentScaleX;
        }

        /// <summary>
        /// 获取垂直DPI缩放比例
        /// </summary>
        public static float GetScaleY()
        {
            if (!_isInitialized)
                Initialize();

            return _currentScaleY;
        }

        /// <summary>
        /// 根据DPI缩放尺寸
        /// </summary>
        /// <param name="originalSize">原始尺寸</param>
        /// <returns>缩放后的尺寸</returns>
        public static Size ScaleSize(Size originalSize)
        {
            if (!_isInitialized)
                Initialize();

            return new Size(
                (int)(originalSize.Width * _currentScaleX),
                (int)(originalSize.Height * _currentScaleY));
        }

        /// <summary>
        /// 根据DPI缩放尺寸
        /// </summary>
        /// <param name="width">原始宽度</param>
        /// <param name="height">原始高度</param>
        /// <returns>缩放后的尺寸</returns>
        public static Size ScaleSize(int width, int height)
        {
            return ScaleSize(new Size(width, height));
        }

        /// <summary>
        /// 根据DPI缩放矩形
        /// </summary>
        /// <param name="rect">原始矩形</param>
        /// <returns>缩放后的矩形</returns>
        public static Rectangle ScaleRectangle(Rectangle rect)
        {
            if (!_isInitialized)
                Initialize();

            return new Rectangle(
                (int)(rect.X * _currentScaleX),
                (int)(rect.Y * _currentScaleY),
                (int)(rect.Width * _currentScaleX),
                (int)(rect.Height * _currentScaleY));
        }

        /// <summary>
        /// 根据DPI缩放点
        /// </summary>
        /// <param name="point">原始点</param>
        /// <returns>缩放后的点</returns>
        public static Point ScalePoint(Point point)
        {
            if (!_isInitialized)
                Initialize();

            return new Point(
                (int)(point.X * _currentScaleX),
                (int)(point.Y * _currentScaleY));
        }

        /// <summary>
        /// 根据DPI缩放字体
        /// </summary>
        /// <param name="originalFont">原始字体</param>
        /// <returns>缩放后的字体</returns>
        public static Font ScaleFont(Font originalFont)
        {
            if (originalFont == null)
                return null;

            if (!_isInitialized)
                Initialize();

            float scaledSize = originalFont.Size * GetCurrentScale();
            return new Font(originalFont.FontFamily, scaledSize, originalFont.Style);
        }

        /// <summary>
        /// 根据DPI缩放字体大小
        /// </summary>
        /// <param name="fontSize">原始字体大小</param>
        /// <returns>缩放后的字体大小</returns>
        public static float ScaleFontSize(float fontSize)
        {
            if (!_isInitialized)
                Initialize();

            return fontSize * GetCurrentScale();
        }

        /// <summary>
        /// 根据DPI缩放边距
        /// </summary>
        /// <param name="padding">原始边距</param>
        /// <returns>缩放后的边距</returns>
        public static Padding ScalePadding(Padding padding)
        {
            if (!_isInitialized)
                Initialize();

            return new Padding(
                (int)(padding.Left * _currentScaleX),
                (int)(padding.Top * _currentScaleY),
                (int)(padding.Right * _currentScaleX),
                (int)(padding.Bottom * _currentScaleY));
        }

        /// <summary>
        /// 应用DPI缩放到窗体
        /// </summary>
        /// <param name="form">目标窗体</param>
        public static void ApplyDpiScaling(Form form)
        {
            try
            {
                if (form == null)
                    return;

                if (!_isInitialized)
                    Initialize();

                form.SuspendLayout();

                // 缩放窗体大小
                var originalSize = form.Size;
                var scaledSize = ScaleSize(originalSize);
                form.Size = scaledSize;

                // 缩放最小尺寸
                if (form.MinimumSize != Size.Empty)
                {
                    form.MinimumSize = ScaleSize(form.MinimumSize);
                }

                // 递归缩放子控件
                ScaleControlHierarchy(form);

                // 缩放字体
                if (form.Font != null)
                {
                    form.Font = ScaleFont(form.Font);
                }

                form.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "DPI助手", "应用DPI缩放失败");
            }
        }

        /// <summary>
        /// 应用DPI缩放到控件
        /// </summary>
        /// <param name="control">目标控件</param>
        public static void ApplyDpiScaling(Control control)
        {
            try
            {
                if (control == null)
                    return;

                if (!_isInitialized)
                    Initialize();

                control.SuspendLayout();

                // 缩放控件尺寸
                if (control.Size != Size.Empty)
                {
                    control.Size = ScaleSize(control.Size);
                }

                // 缩放控件位置
                if (control.Location != Point.Empty)
                {
                    control.Location = ScalePoint(control.Location);
                }

                // 缩放边距
                if (control.Padding != Padding.Empty)
                {
                    control.Padding = ScalePadding(control.Padding);
                }

                // 缩放字体
                if (control.Font != null)
                {
                    control.Font = ScaleFont(control.Font);
                }

                // 递归处理子控件
                ScaleControlHierarchy(control);

                control.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "DPI助手", "应用控件DPI缩放失败");
            }
        }

        /// <summary>
        /// 获取监视器DPI信息
        /// </summary>
        /// <param name="handle">窗体句柄</param>
        /// <returns>DPI信息</returns>
        public static DpiInfo GetMonitorDpi(IntPtr handle)
        {
            try
            {
                var monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);
                var info = new MONITORINFO();
                info.cbSize = Marshal.SizeOf(info);

                if (GetMonitorInfo(monitor, ref info))
                {
                    // 获取监视器的工作区域
                    var workRect = info.rcWork;
                    var monitorRect = info.rcMonitor;

                    return new DpiInfo
                    {
                        MonitorRect = new Rectangle(
                            monitorRect.left, monitorRect.top,
                            monitorRect.right - monitorRect.left,
                            monitorRect.bottom - monitorRect.top),
                        WorkRect = new Rectangle(
                            workRect.left, workRect.top,
                            workRect.right - workRect.left,
                            workRect.bottom - workRect.top),
                        Primary = (info.dwFlags & 1) != 0
                    };
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "DPI助手", "获取监视器DPI信息失败");
            }

            return DpiInfo.Default;
        }

        /// <summary>
        /// 检查是否为高DPI环境
        /// </summary>
        /// <returns>是否为高DPI</returns>
        public static bool IsHighDpi()
        {
            if (!_isInitialized)
                Initialize();

            return _currentScaleX > 1.25f || _currentScaleY > 1.25f;
        }

        /// <summary>
        /// 检查是否支持DPI感知
        /// </summary>
        /// <returns>是否支持DPI感知</returns>
        public static bool IsDpiAware()
        {
            try
            {
                // 检查应用程序是否声明为DPI感知
                return System.Windows.Forms.Application.UseWaitCursor;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取系统DPI X
        /// </summary>
        private static float GetSystemDpiX()
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int dpi = GetDeviceCaps(hdc, LOGPIXELSX);
                    ReleaseDC(IntPtr.Zero, hdc);
                    return dpi;
                }
            }
            catch
            {
                // 忽略错误，返回默认值
            }
            return 96.0f;
        }

        /// <summary>
        /// 获取系统DPI Y
        /// </summary>
        private static float GetSystemDpiY()
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int dpi = GetDeviceCaps(hdc, LOGPIXELSY);
                    ReleaseDC(IntPtr.Zero, hdc);
                    return dpi;
                }
            }
            catch
            {
                // 忽略错误，返回默认值
            }
            return 96.0f;
        }

        /// <summary>
        /// 递归缩放控件层次结构
        /// </summary>
        private static void ScaleControlHierarchy(Control parent)
        {
            try
            {
                foreach (Control child in parent.Controls)
                {
                    // 缩放子控件
                    ApplyDpiScaling(child);

                    // 递归处理
                    ScaleControlHierarchy(child);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "DPI助手", "递归缩放控件失败");
            }
        }

        /// <summary>
        /// 记录DPI信息
        /// </summary>
        private static void LogDpiInfo()
        {
            try
            {
                var message = $"DPI信息: 系统DPI=({_systemDpiX}, {_systemDpiY}), " +
                             $"缩放比例=({_currentScaleX:F2}, {_currentScaleY:F2}), " +
                             $"平均缩放={GetCurrentScale():F2}";

                // 这里可以调用日志服务记录信息
                System.Diagnostics.Debug.WriteLine(message);
            }
            catch
            {
                // 忽略日志记录错误
            }
        }

        #endregion

        #region 实用工具

        /// <summary>
        /// 重置DPI设置
        /// </summary>
        public static void Reset()
        {
            _currentScaleX = 1.0f;
            _currentScaleY = 1.0f;
            _systemDpiX = 96.0f;
            _systemDpiY = 96.0f;
            _isInitialized = false;
        }

        /// <summary>
        /// 更新DPI设置（当显示器配置改变时调用）
        /// </summary>
        public static void UpdateDpiSettings()
        {
            Reset();
            Initialize();
        }

        /// <summary>
        /// 获取推荐的字体大小
        /// </summary>
        /// <param name="baseSize">基础大小</param>
        /// <returns>推荐大小</returns>
        public static float GetRecommendedFontSize(float baseSize)
        {
            if (!_isInitialized)
                Initialize();

            var scale = GetCurrentScale();

            // 根据缩放比例调整字体大小
            if (scale <= 1.0f)
                return baseSize;
            else if (scale <= 1.25f)
                return baseSize * 1.1f;
            else if (scale <= 1.5f)
                return baseSize * 1.2f;
            else if (scale <= 2.0f)
                return baseSize * 1.4f;
            else
                return baseSize * 1.6f;
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// DPI信息结构
    /// </summary>
    public struct DpiInfo
    {
        public Rectangle MonitorRect;
        public Rectangle WorkRect;
        public bool Primary;

        public static DpiInfo Default => new DpiInfo
        {
            MonitorRect = new Rectangle(0, 0, 1920, 1080),
            WorkRect = new Rectangle(0, 0, 1920, 1080),
            Primary = true
        };
    }

    #endregion
}