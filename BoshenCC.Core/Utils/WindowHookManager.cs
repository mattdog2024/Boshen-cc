using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// Windows消息钩子管理器
    /// 负责管理和处理Windows窗口事件钩子
    /// </summary>
    public class WindowHookManager : IDisposable
    {
        #region 私有字段

        private readonly ILogger<WindowHookManager> _logger;
        private readonly ConcurrentDictionary<IntPtr, IntPtr> _eventHooks;
        private readonly object _lockObject = new object();

        private bool _disposed = false;
        private Win32Api.WinEventDelegate _winEventDelegate;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化WindowHookManager实例
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public WindowHookManager(ILogger<WindowHookManager> logger = null)
        {
            _logger = logger;
            _eventHooks = new ConcurrentDictionary<IntPtr, IntPtr>();

            // 创建委托对象并保持引用以防止垃圾回收
            _winEventDelegate = new Win32Api.WinEventDelegate(WinEventProc);

            _logger?.LogInformation("WindowHookManager 已初始化");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 为指定窗口设置事件钩子
        /// </summary>
        /// <param name="windowHandle">目标窗口句柄</param>
        /// <returns>是否设置成功</returns>
        public bool SetEventHook(IntPtr windowHandle)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowHookManager));

            if (windowHandle == IntPtr.Zero)
            {
                _logger?.LogWarning("尝试为无效窗口句柄设置事件钩子");
                return false;
            }

            try
            {
                // 获取窗口进程ID
                Win32Api.GetWindowThreadProcessId(windowHandle, out uint processId);

                // 设置事件钩子，监听位置变化、显示/隐藏、销毁等事件
                IntPtr hookHandle = Win32Api.SetWinEventHook(
                    Win32Api.EVENT_OBJECT_LOCATIONCHANGE,
                    Win32Api.EVENT_OBJECT_DESTROY,
                    IntPtr.Zero,
                    _winEventDelegate,
                    processId,
                    0,
                    Win32Api.WINEVENT_OUTOFCONTEXT);

                if (hookHandle != IntPtr.Zero)
                {
                    if (_eventHooks.TryAdd(windowHandle, hookHandle))
                    {
                        _logger?.LogInformation($"已为窗口 {windowHandle} 设置事件钩子 (进程ID: {processId})");
                        return true;
                    }
                    else
                    {
                        // 如果添加失败，移除钩子
                        Win32Api.UnhookWinEvent(hookHandle);
                        _logger?.LogWarning($"窗口 {windowHandle} 的事件钩子已存在");
                        return false;
                    }
                }
                else
                {
                    uint errorCode = Win32Api.GetLastError();
                    string errorMessage = Win32Api.GetLastErrorMessage();
                    _logger?.LogError($"设置窗口 {windowHandle} 事件钩子失败: {errorMessage} (错误代码: {errorCode})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"设置窗口 {windowHandle} 事件钩子时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 移除指定窗口的事件钩子
        /// </summary>
        /// <param name="windowHandle">目标窗口句柄</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveEventHook(IntPtr windowHandle)
        {
            if (_eventHooks.TryRemove(windowHandle, out IntPtr hookHandle))
            {
                try
                {
                    if (Win32Api.UnhookWinEvent(hookHandle))
                    {
                        _logger?.LogInformation($"已移除窗口 {windowHandle} 的事件钩子");
                        return true;
                    }
                    else
                    {
                        uint errorCode = Win32Api.GetLastError();
                        string errorMessage = Win32Api.GetLastErrorMessage();
                        _logger?.LogError($"移除窗口 {windowHandle} 事件钩子失败: {errorMessage} (错误代码: {errorCode})");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"移除窗口 {windowHandle} 事件钩子时发生异常");
                    return false;
                }
            }

            _logger?.LogWarning($"窗口 {windowHandle} 的事件钩子不存在");
            return false;
        }

        /// <summary>
        /// 移除所有事件钩子
        /// </summary>
        public void RemoveAllEventHooks()
        {
            var windowsToRemove = new List<IntPtr>(_eventHooks.Keys);

            foreach (var windowHandle in windowsToRemove)
            {
                RemoveEventHook(windowHandle);
            }

            _logger?.LogInformation($"已移除所有事件钩子，共处理 {windowsToRemove.Count} 个窗口");
        }

        /// <summary>
        /// 获取当前钩子数量
        /// </summary>
        public int HookCount => _eventHooks.Count;

        /// <summary>
        /// 检查是否有窗口的钩子
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>是否存在钩子</returns>
        public bool HasEventHook(IntPtr windowHandle)
        {
            return _eventHooks.ContainsKey(windowHandle);
        }

        /// <summary>
        /// 获取所有已设置钩子的窗口句柄
        /// </summary>
        /// <returns>窗口句柄列表</returns>
        public List<IntPtr> GetHookedWindows()
        {
            return new List<IntPtr>(_eventHooks.Keys);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 窗口位置变化事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowLocationChanged;

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

        /// <summary>
        /// 窗口名称变化事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowNameChanged;

        #endregion

        #region Windows事件处理

        /// <summary>
        /// Windows事件回调函数
        /// </summary>
        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                // 只处理窗口对象事件
                if (idObject != 0 || hWnd == IntPtr.Zero)
                    return;

                // 检查是否是我们关心的窗口
                if (!_eventHooks.ContainsKey(hWnd))
                    return;

                _logger?.LogDebug($"收到窗口事件: 类型={eventType}, 窗口={hWnd}, 线程={dwEventThread}");

                switch (eventType)
                {
                    case Win32Api.EVENT_OBJECT_LOCATIONCHANGE:
                        OnWindowLocationChanged(hWnd);
                        break;

                    case Win32Api.EVENT_OBJECT_SHOW:
                        OnWindowShown(hWnd);
                        break;

                    case Win32Api.EVENT_OBJECT_HIDE:
                        OnWindowHidden(hWnd);
                        break;

                    case Win32Api.EVENT_OBJECT_DESTROY:
                        OnWindowDestroyed(hWnd);
                        break;

                    case Win32Api.EVENT_OBJECT_NAMECHANGE:
                        OnWindowNameChanged(hWnd);
                        break;

                    default:
                        _logger?.LogDebug($"忽略的事件类型: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"处理Windows事件时发生异常: 事件类型={eventType}, 窗口={hWnd}");
            }
        }

        #endregion

        #region 事件触发器

        /// <summary>
        /// 触发窗口位置变化事件
        /// </summary>
        protected virtual void OnWindowLocationChanged(IntPtr windowHandle)
        {
            WindowLocationChanged?.Invoke(this, new WindowEventArgs
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
            // 立即移除该窗口的钩子
            RemoveEventHook(windowHandle);

            WindowDestroyed?.Invoke(this, new WindowEventArgs
            {
                WindowHandle = windowHandle,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 触发窗口名称变化事件
        /// </summary>
        protected virtual void OnWindowNameChanged(IntPtr windowHandle)
        {
            WindowNameChanged?.Invoke(this, new WindowEventArgs
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
                    // 移除所有事件钩子
                    RemoveAllEventHooks();

                    // 清理事件
                    WindowLocationChanged = null;
                    WindowShown = null;
                    WindowHidden = null;
                    WindowDestroyed = null;
                    WindowNameChanged = null;

                    _logger?.LogInformation("WindowHookManager 已释放资源");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "释放WindowHookManager资源时发生异常");
                }
            }
        }

        #endregion
    }

    #region 事件参数

    /// <summary>
    /// 窗口事件参数
    /// </summary>
    public class WindowEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}