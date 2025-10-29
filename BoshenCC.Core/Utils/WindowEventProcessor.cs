using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 窗口事件处理器
    /// 负责处理和分发窗口事件，提供高级的事件过滤和聚合功能
    /// </summary>
    public class WindowEventProcessor : IDisposable
    {
        #region 私有字段

        private readonly ILogger<WindowEventProcessor> _logger;
        private readonly ConcurrentDictionary<IntPtr, WindowEventState> _windowStates;
        private readonly Timer _eventAggregationTimer;
        private readonly object _lockObject = new object();

        private bool _disposed = false;
        private TimeSpan _eventAggregationInterval = TimeSpan.FromMilliseconds(50);

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化WindowEventProcessor实例
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public WindowEventProcessor(ILogger<WindowEventProcessor> logger = null)
        {
            _logger = logger;
            _windowStates = new ConcurrentDictionary<IntPtr, WindowEventState>();

            // 创建事件聚合定时器
            _eventAggregationTimer = new Timer(ProcessAggregatedEvents, null, Timeout.Infinite, Timeout.Infinite);

            _logger?.LogInformation("WindowEventProcessor 已初始化");
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取或设置事件聚合间隔
        /// </summary>
        public TimeSpan EventAggregationInterval
        {
            get => _eventAggregationInterval;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("事件聚合间隔必须大于零", nameof(value));

                _eventAggregationInterval = value;

                // 如果定时器正在运行，重新设置间隔
                if (_eventAggregationTimer != null)
                {
                    _eventAggregationTimer.Change(_eventAggregationInterval, _eventAggregationInterval);
                }

                _logger?.LogInformation($"事件聚合间隔已设置为: {_eventAggregationInterval.TotalMilliseconds}ms");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始事件处理
        /// </summary>
        public void Start()
        {
            lock (_lockObject)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WindowEventProcessor));

                _eventAggregationTimer.Change(_eventAggregationInterval, _eventAggregationInterval);
                _logger?.LogInformation("WindowEventProcessor 已启动");
            }
        }

        /// <summary>
        /// 停止事件处理
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                _eventAggregationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger?.LogInformation("WindowEventProcessor 已停止");
            }
        }

        /// <summary>
        /// 处理窗口位置变化事件
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public void ProcessLocationChange(IntPtr windowHandle)
        {
            if (_disposed || windowHandle == IntPtr.Zero)
                return;

            try
            {
                var state = GetOrCreateWindowState(windowHandle);
                var currentPosition = GetCurrentWindowPosition(windowHandle);

                lock (state.Lock)
                {
                    // 检查是否是真实的位置变化
                    if (currentPosition != state.LastPosition)
                    {
                        state.HasPendingPositionChange = true;
                        state.NewPosition = currentPosition;
                        state.LastUpdateTime = DateTime.UtcNow;

                        _logger?.LogDebug($"窗口 {windowHandle} 位置变化: {currentPosition.X},{currentPosition.Y} {currentPosition.Width}x{currentPosition.Height}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"处理窗口 {windowHandle} 位置变化事件时发生错误");
            }
        }

        /// <summary>
        /// 处理窗口显示事件
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public void ProcessWindowShow(IntPtr windowHandle)
        {
            if (_disposed || windowHandle == IntPtr.Zero)
                return;

            try
            {
                var state = GetOrCreateWindowState(windowHandle);

                lock (state.Lock)
                {
                    state.HasPendingVisibilityChange = true;
                    state.NewVisibilityState = true;
                    state.LastUpdateTime = DateTime.UtcNow;

                    _logger?.LogDebug($"窗口 {windowHandle} 已显示");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"处理窗口 {windowHandle} 显示事件时发生错误");
            }
        }

        /// <summary>
        /// 处理窗口隐藏事件
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public void ProcessWindowHide(IntPtr windowHandle)
        {
            if (_disposed || windowHandle == IntPtr.Zero)
                return;

            try
            {
                var state = GetOrCreateWindowState(windowHandle);

                lock (state.Lock)
                {
                    state.HasPendingVisibilityChange = true;
                    state.NewVisibilityState = false;
                    state.LastUpdateTime = DateTime.UtcNow;

                    _logger?.LogDebug($"窗口 {windowHandle} 已隐藏");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"处理窗口 {windowHandle} 隐藏事件时发生错误");
            }
        }

        /// <summary>
        /// 处理窗口销毁事件
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public void ProcessWindowDestroy(IntPtr windowHandle)
        {
            if (_disposed || windowHandle == IntPtr.Zero)
                return;

            try
            {
                // 立即触发销毁事件
                OnWindowDestroyed(windowHandle);

                // 清理窗口状态
                _windowStates.TryRemove(windowHandle, out _);

                _logger?.LogInformation($"窗口 {windowHandle} 已销毁");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"处理窗口 {windowHandle} 销毁事件时发生错误");
            }
        }

        /// <summary>
        /// 清理窗口状态
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public void ClearWindowState(IntPtr windowHandle)
        {
            _windowStates.TryRemove(windowHandle, out _);
            _logger?.LogDebug($"已清理窗口 {windowHandle} 的状态");
        }

        /// <summary>
        /// 获取当前跟踪的窗口数量
        /// </summary>
        public int TrackedWindowCount => _windowStates.Count;

        #endregion

        #region 事件

        /// <summary>
        /// 窗口移动事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowMoved;

        /// <summary>
        /// 窗口缩放事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowResized;

        /// <summary>
        /// 窗口显示事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowShown;

        /// <summary>
        /// 窗口隐藏事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowHidden;

        /// <summary>
        /// 窗口销毁事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowDestroyed;

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或创建窗口状态
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>窗口状态</returns>
        private WindowEventState GetOrCreateWindowState(IntPtr windowHandle)
        {
            return _windowStates.GetOrAdd(windowHandle, _ => new WindowEventState
            {
                WindowHandle = windowHandle,
                LastPosition = GetCurrentWindowPosition(windowHandle),
                IsVisible = Win32Api.IsWindowVisible(windowHandle),
                CreatedTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 获取窗口当前位置
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>窗口位置</returns>
        private WindowPosition GetCurrentWindowPosition(IntPtr windowHandle)
        {
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
            catch
            {
                // 忽略错误，返回空位置
            }

            return WindowPosition.Empty;
        }

        /// <summary>
        /// 处理聚合事件
        /// </summary>
        /// <param name="state">状态对象</param>
        private void ProcessAggregatedEvents(object state)
        {
            if (_disposed)
                return;

            try
            {
                var windowsToProcess = new List<IntPtr>(_windowStates.Keys);

                foreach (var windowHandle in windowsToProcess)
                {
                    if (_windowStates.TryGetValue(windowHandle, out var windowState))
                    {
                        ProcessWindowEvents(windowState);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理聚合事件时发生错误");
            }
        }

        /// <summary>
        /// 处理单个窗口的事件
        /// </summary>
        /// <param name="windowState">窗口状态</param>
        private void ProcessWindowEvents(WindowEventState windowState)
        {
            lock (windowState.Lock)
            {
                try
                {
                    // 检查超时的状态（超过5秒未更新的状态）
                    if (DateTime.UtcNow - windowState.LastUpdateTime > TimeSpan.FromSeconds(5))
                    {
                        _windowStates.TryRemove(windowState.WindowHandle, out _);
                        _logger?.LogDebug($"清理超时的窗口状态: {windowState.WindowHandle}");
                        return;
                    }

                    // 处理位置变化
                    if (windowState.HasPendingPositionChange)
                    {
                        ProcessPositionChange(windowState);
                    }

                    // 处理可见性变化
                    if (windowState.HasPendingVisibilityChange)
                    {
                        ProcessVisibilityChange(windowState);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"处理窗口 {windowState.WindowHandle} 事件时发生错误");
                }
            }
        }

        /// <summary>
        /// 处理位置变化
        /// </summary>
        /// <param name="windowState">窗口状态</param>
        private void ProcessPositionChange(WindowEventState windowState)
        {
            var oldPosition = windowState.LastPosition;
            var newPosition = windowState.NewPosition;

            // 检查是否是移动还是缩放
            bool isMove = newPosition.X != oldPosition.X || newPosition.Y != oldPosition.Y;
            bool isResize = newPosition.Width != oldPosition.Width || newPosition.Height != oldPosition.Height;

            if (isMove || isResize)
            {
                windowState.LastPosition = newPosition;

                if (isMove)
                {
                    OnWindowMoved(windowState.WindowHandle);
                }

                if (isResize)
                {
                    OnWindowResized(windowState.WindowHandle);
                }
            }

            windowState.HasPendingPositionChange = false;
        }

        /// <summary>
        /// 处理可见性变化
        /// </summary>
        /// <param name="windowState">窗口状态</param>
        private void ProcessVisibilityChange(WindowEventState windowState)
        {
            var newVisibility = windowState.NewVisibilityState;

            if (newVisibility != windowState.IsVisible)
            {
                windowState.IsVisible = newVisibility;

                if (newVisibility)
                {
                    OnWindowShown(windowState.WindowHandle);
                }
                else
                {
                    OnWindowHidden(windowState.WindowHandle);
                }
            }

            windowState.HasPendingVisibilityChange = false;
        }

        #endregion

        #region 事件触发器

        /// <summary>
        /// 触发窗口移动事件
        /// </summary>
        protected virtual void OnWindowMoved(IntPtr windowHandle)
        {
            WindowMoved?.Invoke(this, new WindowEventArgs
            {
                WindowHandle = windowHandle,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口缩放事件
        /// </summary>
        protected virtual void OnWindowResized(IntPtr windowHandle)
        {
            WindowResized?.Invoke(this, new WindowEventArgs
            {
                WindowHandle = windowHandle,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口显示事件
        /// </summary>
        protected virtual void OnWindowShown(IntPtr windowHandle)
        {
            WindowShown?.Invoke(this, new WindowEventArgs
            {
                WindowHandle = windowHandle,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口隐藏事件
        /// </summary>
        protected virtual void OnWindowHidden(IntPtr windowHandle)
        {
            WindowHidden?.Invoke(this, new WindowEventArgs
            {
                WindowHandle = windowHandle,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口销毁事件
        /// </summary>
        protected virtual void OnWindowDestroyed(IntPtr windowHandle)
        {
            WindowDestroyed?.Invoke(this, new WindowEventArgs
            {
                WindowHandle = windowHandle,
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

                try
                {
                    // 停止定时器
                    _eventAggregationTimer?.Dispose();

                    // 清理事件
                    WindowMoved = null;
                    WindowResized = null;
                    WindowShown = null;
                    WindowHidden = null;
                    WindowDestroyed = null;

                    // 清理窗口状态
                    _windowStates.Clear();

                    _logger?.LogInformation("WindowEventProcessor 已释放资源");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "释放WindowEventProcessor资源时发生异常");
                }
            }
        }

        #endregion
    }

    #region 内部数据结构

    /// <summary>
    /// 窗口事件状态
    /// </summary>
    internal class WindowEventState
    {
        public IntPtr WindowHandle { get; set; }
        public WindowPosition LastPosition { get; set; }
        public bool IsVisible { get; set; }
        public bool HasPendingPositionChange { get; set; }
        public bool HasPendingVisibilityChange { get; set; }
        public WindowPosition NewPosition { get; set; }
        public bool NewVisibilityState { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public object Lock { get; } = new object();
    }

    #endregion
}