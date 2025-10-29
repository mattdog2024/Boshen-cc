using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoshenCC.Core.Utils;
using Microsoft.Extensions.Logging;

namespace BoshenCC.Core.Services
{
    /// <summary>
    /// 窗口跟踪服务
    /// 负责检测、跟踪目标窗口并通知位置变化
    /// </summary>
    public class WindowTracker : IDisposable
    {
        #region 私有字段

        private readonly ILogger<WindowTracker> _logger;
        private readonly WindowHookManager _hookManager;
        private readonly WindowEventProcessor _eventProcessor;
        private readonly ScreenCoordinateHelper _coordinateHelper;

        private readonly ConcurrentDictionary<IntPtr, TrackedWindow> _trackedWindows;
        private readonly Timer _trackingTimer;
        private readonly object _lockObject = new object();

        private bool _disposed = false;
        private bool _isTracking = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化WindowTracker实例
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public WindowTracker(ILogger<WindowTracker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hookManager = new WindowHookManager();
            _eventProcessor = new WindowEventProcessor();
            _coordinateHelper = new ScreenCoordinateHelper();

            _trackedWindows = new ConcurrentDictionary<IntPtr, TrackedWindow>();

            // 设置事件处理器
            _eventProcessor.WindowMoved += OnWindowMoved;
            _eventProcessor.WindowResized += OnWindowResized;
            _eventProcessor.WindowShown += OnWindowShown;
            _eventProcessor.WindowHidden += OnWindowHidden;
            _eventProcessor.WindowDestroyed += OnWindowDestroyed;

            // 创建位置检查定时器（每100ms检查一次）
            _trackingTimer = new Timer(CheckWindowPositions, null, Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("WindowTracker 已初始化");
        }

        #endregion

        #region 公共事件

        /// <summary>
        /// 窗口位置变化事件
        /// </summary>
        public event EventHandler<WindowPositionChangedEventArgs> WindowPositionChanged;

        /// <summary>
        /// 窗口大小变化事件
        /// </summary>
        public event EventHandler<WindowSizeChangedEventArgs> WindowSizeChanged;

        /// <summary>
        /// 窗口可见性变化事件
        /// </summary>
        public event EventHandler<WindowVisibilityChangedEventArgs> WindowVisibilityChanged;

        /// <summary>
        /// 窗口销毁事件
        /// </summary>
        public event EventHandler<WindowDestroyedEventArgs> WindowDestroyed;

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始窗口跟踪
        /// </summary>
        public void StartTracking()
        {
            lock (_lockObject)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WindowTracker));

                if (_isTracking)
                    return;

                _isTracking = true;
                _trackingTimer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

                _logger.LogInformation("窗口跟踪已启动");
            }
        }

        /// <summary>
        /// 停止窗口跟踪
        /// </summary>
        public void StopTracking()
        {
            lock (_lockObject)
            {
                if (!_isTracking)
                    return;

                _isTracking = false;
                _trackingTimer.Change(Timeout.Infinite, Timeout.Infinite);

                _logger.LogInformation("窗口跟踪已停止");
            }
        }

        /// <summary>
        /// 添加窗口到跟踪列表
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="name">窗口名称（可选）</param>
        /// <returns>是否添加成功</returns>
        public bool TrackWindow(IntPtr windowHandle, string name = null)
        {
            if (windowHandle == IntPtr.Zero)
            {
                _logger.LogWarning("尝试跟踪无效的窗口句柄");
                return false;
            }

            try
            {
                // 检查窗口是否存在
                if (!IsWindowValid(windowHandle))
                {
                    _logger.LogWarning($"窗口句柄 {windowHandle} 无效或已销毁");
                    return false;
                }

                var trackedWindow = new TrackedWindow
                {
                    Handle = windowHandle,
                    Name = name ?? GetWindowTitle(windowHandle),
                    ClassName = GetWindowClassName(windowHandle),
                    ProcessId = GetWindowProcessId(windowHandle),
                    IsVisible = Win32Api.IsWindowVisible(windowHandle),
                    LastPosition = GetWindowPosition(windowHandle),
                    LastCheckTime = DateTime.UtcNow
                };

                if (_trackedWindows.TryAdd(windowHandle, trackedWindow))
                {
                    // 设置事件钩子
                    _hookManager.SetEventHook(windowHandle);

                    _logger.LogInformation($"已开始跟踪窗口: {trackedWindow.Name} (句柄: {windowHandle})");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"窗口 {windowHandle} 已在跟踪列表中");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加窗口 {windowHandle} 到跟踪列表时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 从跟踪列表移除窗口
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>是否移除成功</returns>
        public bool UntrackWindow(IntPtr windowHandle)
        {
            if (_trackedWindows.TryRemove(windowHandle, out var trackedWindow))
            {
                _hookManager.RemoveEventHook(windowHandle);

                _logger.LogInformation($"已停止跟踪窗口: {trackedWindow.Name} (句柄: {windowHandle})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 查找并跟踪窗口
        /// </summary>
        /// <param name="className">窗口类名</param>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>找到的窗口句柄，如果未找到则为IntPtr.Zero</returns>
        public IntPtr FindAndTrackWindow(string className = null, string windowTitle = null)
        {
            IntPtr windowHandle = Win32Api.FindWindow(className, windowTitle);

            if (windowHandle != IntPtr.Zero)
            {
                if (TrackWindow(windowHandle))
                {
                    _logger.LogInformation($"找到并开始跟踪窗口: 类名={className}, 标题={windowTitle}");
                }
            }
            else
            {
                _logger.LogWarning($"未找到窗口: 类名={className}, 标题={windowTitle}");
            }

            return windowHandle;
        }

        /// <summary>
        /// 根据标题模糊匹配查找窗口
        /// </summary>
        /// <param name="titlePattern">标题模式</param>
        /// <returns>匹配的窗口句柄列表</returns>
        public List<IntPtr> FindWindowsByTitlePattern(string titlePattern)
        {
            var result = new List<IntPtr>();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowValid(hWnd))
                {
                    string title = GetWindowTitle(hWnd);
                    if (!string.IsNullOrEmpty(title) && title.Contains(titlePattern))
                    {
                        result.Add(hWnd);
                    }
                }
                return true;
            }, IntPtr.Zero);

            _logger.LogInformation($"找到 {result.Count} 个匹配标题模式 '{titlePattern}' 的窗口");
            return result;
        }

        /// <summary>
        /// 获取当前跟踪的窗口信息
        /// </summary>
        /// <returns>窗口信息列表</returns>
        public List<WindowInfo> GetTrackedWindows()
        {
            var result = new List<WindowInfo>();

            foreach (var kvp in _trackedWindows)
            {
                var trackedWindow = kvp.Value;
                if (IsWindowValid(trackedWindow.Handle))
                {
                    var position = GetWindowPosition(trackedWindow.Handle);

                    result.Add(new WindowInfo
                    {
                        Handle = trackedWindow.Handle,
                        Name = trackedWindow.Name,
                        ClassName = trackedWindow.ClassName,
                        ProcessId = trackedWindow.ProcessId,
                        IsVisible = Win32Api.IsWindowVisible(trackedWindow.Handle),
                        Position = position,
                        IsMinimized = Win32Api.IsIconic(trackedWindow.Handle),
                        IsMaximized = Win32Api.IsZoomed(trackedWindow.Handle)
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 获取窗口当前位置
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>窗口位置信息</returns>
        public WindowPosition GetWindowPosition(IntPtr windowHandle)
        {
            if (!IsWindowValid(windowHandle))
                return WindowPosition.Empty;

            try
            {
                if (Win32Api.GetWindowRect(windowHandle, out WindowStructs.RECT rect))
                {
                    return new WindowPosition
                    {
                        X = rect.Left,
                        Y = rect.Top,
                        Width = rect.Width,
                        Height = rect.Height
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取窗口 {windowHandle} 位置时发生错误");
            }

            return WindowPosition.Empty;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查窗口是否有效
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>窗口是否有效</returns>
        private bool IsWindowValid(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && Win32Api.IsWindowVisible(windowHandle);
        }

        /// <summary>
        /// 获取窗口标题
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>窗口标题</returns>
        private string GetWindowTitle(IntPtr windowHandle)
        {
            try
            {
                int length = Win32Api.GetWindowTextLength(windowHandle);
                if (length == 0)
                    return string.Empty;

                var builder = new System.Text.StringBuilder(length + 1);
                Win32Api.GetWindowText(windowHandle, builder, builder.Capacity);
                return builder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取窗口类名
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>窗口类名</returns>
        private string GetWindowClassName(IntPtr windowHandle)
        {
            try
            {
                var builder = new System.Text.StringBuilder(256);
                Win32Api.GetClassName(windowHandle, builder, builder.Capacity);
                return builder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取窗口进程ID
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>进程ID</returns>
        private uint GetWindowProcessId(IntPtr windowHandle)
        {
            try
            {
                Win32Api.GetWindowThreadProcessId(windowHandle, out uint processId);
                return processId;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 定时检查窗口位置
        /// </summary>
        /// <param name="state">状态对象</param>
        private void CheckWindowPositions(object state)
        {
            if (!_isTracking || _disposed)
                return;

            try
            {
                var windowsToRemove = new List<IntPtr>();

                foreach (var kvp in _trackedWindows)
                {
                    var handle = kvp.Key;
                    var trackedWindow = kvp.Value;

                    // 检查窗口是否仍然有效
                    if (!IsWindowValid(handle))
                    {
                        windowsToRemove.Add(handle);
                        continue;
                    }

                    // 获取当前位置
                    var currentPosition = GetWindowPosition(handle);

                    // 检查位置是否发生变化
                    if (currentPosition != trackedWindow.LastPosition)
                    {
                        var oldPosition = trackedWindow.LastPosition;
                        trackedWindow.LastPosition = currentPosition;
                        trackedWindow.LastCheckTime = DateTime.UtcNow;

                        // 检查是移动还是缩放
                        if (currentPosition.X != oldPosition.X || currentPosition.Y != oldPosition.Y)
                        {
                            OnWindowPositionChanged(handle, currentPosition, oldPosition);
                        }

                        if (currentPosition.Width != oldPosition.Width || currentPosition.Height != oldPosition.Height)
                        {
                            OnWindowSizeChanged(handle, currentPosition, oldPosition);
                        }
                    }

                    // 检查可见性变化
                    var isVisible = Win32Api.IsWindowVisible(handle);
                    if (isVisible != trackedWindow.IsVisible)
                    {
                        trackedWindow.IsVisible = isVisible;
                        OnWindowVisibilityChanged(handle, isVisible);
                    }
                }

                // 移除无效窗口
                foreach (var handle in windowsToRemove)
                {
                    UntrackWindow(handle);
                    OnWindowDestroyed(handle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查窗口位置时发生错误");
            }
        }

        #endregion

        #region 事件处理器

        /// <summary>
        /// 窗口移动事件处理
        /// </summary>
        private void OnWindowMoved(object sender, WindowEventArgs e)
        {
            if (_trackedWindows.TryGetValue(e.WindowHandle, out var trackedWindow))
            {
                var position = GetWindowPosition(e.WindowHandle);
                var oldPosition = trackedWindow.LastPosition;
                trackedWindow.LastPosition = position;

                OnWindowPositionChanged(e.WindowHandle, position, oldPosition);
            }
        }

        /// <summary>
        /// 窗口缩放事件处理
        /// </summary>
        private void OnWindowResized(object sender, WindowEventArgs e)
        {
            if (_trackedWindows.TryGetValue(e.WindowHandle, out var trackedWindow))
            {
                var position = GetWindowPosition(e.WindowHandle);
                var oldPosition = trackedWindow.LastPosition;
                trackedWindow.LastPosition = position;

                OnWindowSizeChanged(e.WindowHandle, position, oldPosition);
            }
        }

        /// <summary>
        /// 窗口显示事件处理
        /// </summary>
        private void OnWindowShown(object sender, WindowEventArgs e)
        {
            if (_trackedWindows.TryGetValue(e.WindowHandle, out var trackedWindow))
            {
                trackedWindow.IsVisible = true;
                OnWindowVisibilityChanged(e.WindowHandle, true);
            }
        }

        /// <summary>
        /// 窗口隐藏事件处理
        /// </summary>
        private void OnWindowHidden(object sender, WindowEventArgs e)
        {
            if (_trackedWindows.TryGetValue(e.WindowHandle, out var trackedWindow))
            {
                trackedWindow.IsVisible = false;
                OnWindowVisibilityChanged(e.WindowHandle, false);
            }
        }

        /// <summary>
        /// 窗口销毁事件处理
        /// </summary>
        private void OnWindowDestroyed(object sender, WindowEventArgs e)
        {
            UntrackWindow(e.WindowHandle);
            OnWindowDestroyed(e.WindowHandle);
        }

        /// <summary>
        /// 触发窗口位置变化事件
        /// </summary>
        protected virtual void OnWindowPositionChanged(IntPtr handle, WindowPosition newPosition, WindowPosition oldPosition)
        {
            WindowPositionChanged?.Invoke(this, new WindowPositionChangedEventArgs
            {
                WindowHandle = handle,
                NewPosition = newPosition,
                OldPosition = oldPosition,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口大小变化事件
        /// </summary>
        protected virtual void OnWindowSizeChanged(IntPtr handle, WindowPosition newPosition, WindowPosition oldPosition)
        {
            WindowSizeChanged?.Invoke(this, new WindowSizeChangedEventArgs
            {
                WindowHandle = handle,
                NewSize = new WindowSize(newPosition.Width, newPosition.Height),
                OldSize = new WindowSize(oldPosition.Width, oldPosition.Height),
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口可见性变化事件
        /// </summary>
        protected virtual void OnWindowVisibilityChanged(IntPtr handle, bool isVisible)
        {
            WindowVisibilityChanged?.Invoke(this, new WindowVisibilityChangedEventArgs
            {
                WindowHandle = handle,
                IsVisible = isVisible,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口销毁事件
        /// </summary>
        protected virtual void OnWindowDestroyed(IntPtr handle)
        {
            WindowDestroyed?.Invoke(this, new WindowDestroyedEventArgs
            {
                WindowHandle = handle,
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                if (_disposed)
                    return;

                _disposed = true;

                StopTracking();

                _trackingTimer?.Dispose();
                _hookManager?.Dispose();
                _eventProcessor?.Dispose();

                _trackedWindows.Clear();

                _logger.LogInformation("WindowTracker 已释放资源");
            }
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 跟踪窗口信息
    /// </summary>
    internal class TrackedWindow
    {
        public IntPtr Handle { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public uint ProcessId { get; set; }
        public bool IsVisible { get; set; }
        public WindowPosition LastPosition { get; set; }
        public DateTime LastCheckTime { get; set; }
    }

    /// <summary>
    /// 窗口位置信息
    /// </summary>
    public class WindowPosition : IEquatable<WindowPosition>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public static WindowPosition Empty => new WindowPosition();

        public bool Equals(WindowPosition other)
        {
            return other != null && X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WindowPosition);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public static bool operator ==(WindowPosition left, WindowPosition right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WindowPosition left, WindowPosition right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// 窗口尺寸信息
    /// </summary>
    public class WindowSize : IEquatable<WindowSize>
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public WindowSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(WindowSize other)
        {
            return other != null && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WindowSize);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }
    }

    /// <summary>
    /// 窗口信息
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public uint ProcessId { get; set; }
        public bool IsVisible { get; set; }
        public WindowPosition Position { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsMaximized { get; set; }
    }

    #endregion

    #region 事件参数

    /// <summary>
    /// 窗口位置变化事件参数
    /// </summary>
    public class WindowPositionChangedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public WindowPosition NewPosition { get; set; }
        public WindowPosition OldPosition { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 窗口大小变化事件参数
    /// </summary>
    public class WindowSizeChangedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public WindowSize NewSize { get; set; }
        public WindowSize OldSize { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 窗口可见性变化事件参数
    /// </summary>
    public class WindowVisibilityChangedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public bool IsVisible { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 窗口销毁事件参数
    /// </summary>
    public class WindowDestroyedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}