using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using BoshenCC.Services.Interfaces;
using BoshenCC.Core.Utils;

namespace BoshenCC.Core.Services
{
    /// <summary>
    /// 窗口管理服务实现
    /// 负责透明窗口的创建、管理和生命周期控制
    /// </summary>
    public class WindowManagerService : IWindowManagerService, IDisposable
    {
        private readonly ILogService _logService;
        private readonly ConcurrentDictionary<string, WindowInfo> _windows;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        #region 内部类

        /// <summary>
        /// 窗口信息
        /// </summary>
        private class WindowInfo
        {
            public string Id { get; }
            public string Name { get; }
            public TransparentWindow Window { get; }
            public DateTime CreatedAt { get; }

            public WindowInfo(string id, string name, TransparentWindow window)
            {
                Id = id;
                Name = name;
                Window = window;
                CreatedAt = DateTime.Now;
            }
        }

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化窗口管理服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        public WindowManagerService(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _windows = new ConcurrentDictionary<string, WindowInfo>();
            _logService.Info("WindowManagerService 初始化完成");
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~WindowManagerService()
        {
            Dispose(false);
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取窗口数量
        /// </summary>
        public int WindowCount => _windows.Count;

        #endregion

        #region 事件

        /// <summary>
        /// 窗口创建事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowCreated;

        /// <summary>
        /// 窗口销毁事件
        /// </summary>
        public event EventHandler<WindowEventArgs> WindowDestroyed;

        /// <summary>
        /// 窗口属性更改事件
        /// </summary>
        public event EventHandler<WindowPropertyChangedEventArgs> WindowPropertyChanged;

        #endregion

        #region 窗口创建和销毁

        /// <summary>
        /// 创建新的透明窗口
        /// </summary>
        /// <param name="name">窗口名称</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="alpha">透明度 (0-255)</param>
        /// <param name="clickThrough">是否支持点击穿透</param>
        /// <returns>窗口ID，创建失败返回null</returns>
        public string CreateWindow(string name, int x, int y, int width, int height, byte alpha = 255, bool clickThrough = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("窗口名称不能为空", nameof(name));
                }

                if (width <= 0 || height <= 0)
                {
                    throw new ArgumentException("窗口宽度和高度必须大于0");
                }

                _logService.Info($"开始创建透明窗口: {name}, 位置({x},{y}), 尺寸({width}x{height})");

                // 生成唯一窗口ID
                string windowId = GenerateWindowId(name);

                // 创建透明窗口实例
                var window = new TransparentWindow(_logService);

                // 创建窗口
                if (!window.Create(x, y, width, height, alpha, clickThrough))
                {
                    _logService.Error($"创建透明窗口失败: {name}");
                    window.Dispose();
                    return null;
                }

                // 添加到管理列表
                var windowInfo = new WindowInfo(windowId, name, window);
                _windows.TryAdd(windowId, windowInfo);

                // 触发窗口创建事件
                OnWindowCreated(new WindowEventArgs(windowId, name));

                _logService.Info($"透明窗口创建成功: {name} (ID: {windowId})");
                return windowId;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, $"创建透明窗口失败: {name}");
                return null;
            }
        }

        /// <summary>
        /// 销毁指定的透明窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>是否销毁成功</returns>
        public bool DestroyWindow(string windowId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(windowId))
                {
                    return false;
                }

                if (!_windows.TryRemove(windowId, out var windowInfo))
                {
                    _logService.Warn($"窗口不存在: {windowId}");
                    return false;
                }

                _logService.Info($"开始销毁透明窗口: {windowInfo.Name} (ID: {windowId})");

                // 销毁窗口
                windowInfo.Window?.Dispose();

                // 触发窗口销毁事件
                OnWindowDestroyed(new WindowEventArgs(windowId, windowInfo.Name));

                _logService.Info($"透明窗口销毁成功: {windowInfo.Name} (ID: {windowId})");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, $"销毁透明窗口失败: {windowId}");
                return false;
            }
        }

        /// <summary>
        /// 销毁所有透明窗口
        /// </summary>
        public void DestroyAllWindows()
        {
            try
            {
                _logService.Info("开始销毁所有透明窗口");

                var windowIds = _windows.Keys.ToList();
                foreach (var windowId in windowIds)
                {
                    DestroyWindow(windowId);
                }

                _logService.Info("所有透明窗口销毁完成");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "销毁所有透明窗口失败");
            }
        }

        #endregion

        #region 窗口获取和查询

        /// <summary>
        /// 获取透明窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>透明窗口实例，不存在返回null</returns>
        public TransparentWindow GetWindow(string windowId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(windowId))
                {
                    return null;
                }

                return _windows.TryGetValue(windowId, out var windowInfo) ? windowInfo.Window : null;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, $"获取透明窗口失败: {windowId}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有窗口ID列表
        /// </summary>
        /// <returns>窗口ID列表</returns>
        public IEnumerable<string> GetAllWindowIds()
        {
            try
            {
                return _windows.Keys.ToList();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "获取所有窗口ID失败");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// 检查窗口是否存在
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>窗口是否存在</returns>
        public bool WindowExists(string windowId)
        {
            return !string.IsNullOrWhiteSpace(windowId) && _windows.ContainsKey(windowId);
        }

        #endregion

        #region 窗口操作

        /// <summary>
        /// 显示窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>是否成功</returns>
        public bool ShowWindow(string windowId)
        {
            return ExecuteWindowOperation(windowId, window =>
            {
                window.Show();
                return true;
            }, "显示窗口");
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>是否成功</returns>
        public bool HideWindow(string windowId)
        {
            return ExecuteWindowOperation(windowId, window =>
            {
                window.Hide();
                return true;
            }, "隐藏窗口");
        }

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否成功</returns>
        public bool SetWindowPosition(string windowId, int x, int y)
        {
            return ExecuteWindowOperation(windowId, window =>
            {
                var oldPosition = window.Position;
                window.SetPosition(x, y);
                OnWindowPropertyChanged(new WindowPropertyChangedEventArgs(windowId, GetWindowName(windowId), "Position", oldPosition, new System.Drawing.Point(x, y)));
                return true;
            }, "设置窗口位置");
        }

        /// <summary>
        /// 设置窗口尺寸
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>是否成功</returns>
        public bool SetWindowSize(string windowId, int width, int height)
        {
            return ExecuteWindowOperation(windowId, window =>
            {
                var oldSize = window.Size;
                window.SetSize(width, height);
                OnWindowPropertyChanged(new WindowPropertyChangedEventArgs(windowId, GetWindowName(windowId), "Size", oldSize, new System.Drawing.Size(width, height)));
                return true;
            }, "设置窗口尺寸");
        }

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="alpha">透明度 (0-255)</param>
        /// <returns>是否成功</returns>
        public bool SetWindowAlpha(string windowId, byte alpha)
        {
            return ExecuteWindowOperation(windowId, window =>
            {
                var oldAlpha = window.Alpha;
                window.SetAlpha(alpha);
                OnWindowPropertyChanged(new WindowPropertyChangedEventArgs(windowId, GetWindowName(windowId), "Alpha", oldAlpha, alpha));
                return true;
            }, "设置窗口透明度");
        }

        /// <summary>
        /// 设置窗口点击穿透
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="enabled">是否启用点击穿透</param>
        /// <returns>是否成功</returns>
        public bool SetWindowClickThrough(string windowId, bool enabled)
        {
            return ExecuteWindowOperation(windowId, window =>
            {
                var oldValue = window.IsClickThrough;
                window.SetClickThrough(enabled);
                OnWindowPropertyChanged(new WindowPropertyChangedEventArgs(windowId, GetWindowName(windowId), "ClickThrough", oldValue, enabled));
                return true;
            }, "设置窗口点击穿透");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 生成唯一窗口ID
        /// </summary>
        /// <param name="name">窗口名称</param>
        /// <returns>唯一窗口ID</returns>
        private string GenerateWindowId(string name)
        {
            return $"{name}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// 获取窗口名称
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <returns>窗口名称</returns>
        private string GetWindowName(string windowId)
        {
            return _windows.TryGetValue(windowId, out var windowInfo) ? windowInfo.Name : windowId;
        }

        /// <summary>
        /// 执行窗口操作
        /// </summary>
        /// <param name="windowId">窗口ID</param>
        /// <param name="operation">操作</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>是否成功</returns>
        private bool ExecuteWindowOperation(string windowId, Func<TransparentWindow, bool> operation, string operationName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(windowId))
                {
                    _logService.Warn($"{operationName}失败: 窗口ID为空");
                    return false;
                }

                if (!_windows.TryGetValue(windowId, out var windowInfo))
                {
                    _logService.Warn($"{operationName}失败: 窗口不存在 - {windowId}");
                    return false;
                }

                return operation(windowInfo.Window);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, $"{operationName}失败: {windowId}");
                return false;
            }
        }

        #endregion

        #region 事件触发

        /// <summary>
        /// 触发窗口创建事件
        /// </summary>
        protected virtual void OnWindowCreated(WindowEventArgs e)
        {
            try
            {
                WindowCreated?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "触发窗口创建事件失败");
            }
        }

        /// <summary>
        /// 触发窗口销毁事件
        /// </summary>
        protected virtual void OnWindowDestroyed(WindowEventArgs e)
        {
            try
            {
                WindowDestroyed?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "触发窗口销毁事件失败");
            }
        }

        /// <summary>
        /// 触发窗口属性更改事件
        /// </summary>
        protected virtual void OnWindowPropertyChanged(WindowPropertyChangedEventArgs e)
        {
            try
            {
                WindowPropertyChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "触发窗口属性更改事件失败");
            }
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logService.Info("WindowManagerService 正在释放托管资源");
                }

                // 销毁所有窗口
                DestroyAllWindows();

                // 清理事件
                WindowCreated = null;
                WindowDestroyed = null;
                WindowPropertyChanged = null;

                _disposed = true;
                _logService.Info("WindowManagerService 资源释放完成");
            }
        }

        #endregion
    }
}