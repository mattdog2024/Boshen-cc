using System;
using System.Drawing;
using System.Runtime.InteropServices;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 透明窗口封装类
    /// 提供分层透明窗口的创建、管理和销毁功能，支持点击穿透
    /// </summary>
    public class TransparentWindow : IDisposable
    {
        private readonly ILogService _logService;
        private IntPtr _windowHandle = IntPtr.Zero;
        private IntPtr _deviceContext = IntPtr.Zero;
        private IntPtr _bitmapHandle = IntPtr.Zero;
        private IntPtr _oldBitmapHandle = IntPtr.Zero;
        private bool _isDisposed = false;
        private bool _isVisible = false;

        #region 属性

        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle => _windowHandle;

        /// <summary>
        /// 窗口位置
        /// </summary>
        public Point Position { get; private set; }

        /// <summary>
        /// 窗口尺寸
        /// </summary>
        public Size Size { get; private set; }

        /// <summary>
        /// 透明度 (0-255)
        /// </summary>
        public byte Alpha { get; private set; } = 255;

        /// <summary>
        /// 是否支持点击穿透
        /// </summary>
        public bool IsClickThrough { get; private set; } = true;

        /// <summary>
        /// 窗口是否可见
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// 窗口是否已创建
        /// </summary>
        public bool IsCreated => _windowHandle != IntPtr.Zero;

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化透明窗口
        /// </summary>
        /// <param name="logService">日志服务</param>
        public TransparentWindow(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _logService.LogInfo("TransparentWindow 初始化");
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~TransparentWindow()
        {
            Dispose(false);
        }

        #endregion

        #region 窗口创建和销毁

        /// <summary>
        /// 创建透明窗口
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="alpha">透明度 (0-255)</param>
        /// <param name="clickThrough">是否支持点击穿透</param>
        /// <returns>是否创建成功</returns>
        public bool Create(int x, int y, int width, int height, byte alpha = 255, bool clickThrough = true)
        {
            try
            {
                if (IsCreated)
                {
                    _logService.LogWarning("透明窗口已经创建，先销毁现有窗口");
                    Destroy();
                }

                // 验证参数
                if (width <= 0 || height <= 0)
                {
                    throw new ArgumentException("窗口宽度和高度必须大于0");
                }

                _logService.LogInfo($"开始创建透明窗口: 位置({x},{y}), 尺寸({width}x{height}), 透明度({alpha})");

                // 设置窗口属性
                Position = new Point(x, y);
                Size = new Size(width, height);
                Alpha = alpha;
                IsClickThrough = clickThrough;

                // 注册窗口类
                if (!RegisterWindowClass())
                {
                    _logService.LogError("注册窗口类失败");
                    return false;
                }

                // 创建窗口
                return CreateWindow();
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建透明窗口失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 销毁透明窗口
        /// </summary>
        public void Destroy()
        {
            try
            {
                if (_windowHandle != IntPtr.Zero)
                {
                    _logService.LogInfo("开始销毁透明窗口");

                    // 释放绘图资源
                    ReleaseDrawingResources();

                    // 销毁窗口
                    Win32Api.CheckWin32Result(Win32Api.DestroyWindow(_windowHandle), "DestroyWindow");
                    _windowHandle = IntPtr.Zero;
                    _isVisible = false;

                    _logService.LogInfo("透明窗口销毁成功");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"销毁透明窗口失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 窗口显示和隐藏

        /// <summary>
        /// 显示窗口
        /// </summary>
        public void Show()
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                if (!_isVisible)
                {
                    Win32Api.CheckWin32Result(
                        Win32Api.ShowWindow(_windowHandle, Win32Constants.SW_SHOWNOACTIVATE),
                        "ShowWindow");

                    _isVisible = true;
                    _logService.LogInfo("透明窗口已显示");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"显示透明窗口失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void Hide()
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                if (_isVisible)
                {
                    Win32Api.CheckWin32Result(
                        Win32Api.ShowWindow(_windowHandle, Win32Constants.SW_HIDE),
                        "ShowWindow");

                    _isVisible = false;
                    _logService.LogInfo("透明窗口已隐藏");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"隐藏透明窗口失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 窗口属性更新

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        public void SetPosition(int x, int y)
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                Win32Api.CheckWin32Result(
                    Win32Api.SetWindowPos(_windowHandle, IntPtr.Zero, x, y, 0, 0,
                        Win32Constants.SWP_NOSIZE | Win32Constants.SWP_NOZORDER | Win32Constants.SWP_NOACTIVATE),
                    "SetWindowPos");

                Position = new Point(x, y);
                _logService.LogDebug($"窗口位置已更新为: ({x},{y})");
            }
            catch (Exception ex)
            {
                _logService.LogError($"设置窗口位置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置窗口尺寸
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void SetSize(int width, int height)
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                if (width <= 0 || height <= 0)
                {
                    throw new ArgumentException("窗口宽度和高度必须大于0");
                }

                Win32Api.CheckWin32Result(
                    Win32Api.SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, width, height,
                        Win32Constants.SWP_NOMOVE | Win32Constants.SWP_NOZORDER | Win32Constants.SWP_NOACTIVATE),
                    "SetWindowPos");

                Size = new Size(width, height);

                // 重新创建绘图资源
                RecreateDrawingResources();

                _logService.LogDebug($"窗口尺寸已更新为: ({width}x{height})");
            }
            catch (Exception ex)
            {
                _logService.LogError($"设置窗口尺寸失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="alpha">透明度 (0-255)</param>
        public void SetAlpha(byte alpha)
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                Win32Api.CheckWin32Result(
                    Win32Api.SetLayeredWindowAttributes(_windowHandle, 0, alpha, Win32Constants.LWA_ALPHA),
                    "SetLayeredWindowAttributes");

                Alpha = alpha;
                _logService.LogDebug($"窗口透明度已更新为: {alpha}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"设置窗口透明度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置点击穿透
        /// </summary>
        /// <param name="enabled">是否启用点击穿透</param>
        public void SetClickThrough(bool enabled)
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                uint exStyle = (uint)Win32Api.GetWindowLong(_windowHandle, Win32Constants.GWL_EXSTYLE);

                if (enabled)
                {
                    exStyle |= Win32Constants.WS_EX_TRANSPARENT;
                }
                else
                {
                    exStyle &= ~Win32Constants.WS_EX_TRANSPARENT;
                }

                Win32Api.SetWindowLong(_windowHandle, Win32Constants.GWL_EXSTYLE, (int)exStyle);
                IsClickThrough = enabled;

                _logService.LogDebug($"点击穿透已{(enabled ? "启用" : "禁用")}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"设置点击穿透失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 绘图功能

        /// <summary>
        /// 获取设备上下文用于绘图
        /// </summary>
        /// <returns>设备上下文句柄</returns>
        public IntPtr GetDeviceContext()
        {
            try
            {
                if (!IsCreated)
                {
                    throw new InvalidOperationException("窗口尚未创建");
                }

                if (_deviceContext == IntPtr.Zero)
                {
                    CreateDrawingResources();
                }

                return _deviceContext;
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取设备上下文失败: {ex.Message}", ex);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 更新分层窗口显示
        /// </summary>
        public void UpdateWindow()
        {
            try
            {
                if (!IsCreated || _deviceContext == IntPtr.Zero)
                {
                    return;
                }

                var point = new WindowStructs.POINT(Position.X, Position.Y);
                var size = new WindowStructs.SIZE(Size.Width, Size.Height);
                var srcPoint = new WindowStructs.POINT(0, 0);
                var blendFunc = WindowStructs.BLENDFUNCTION.CreateDefault(Alpha);

                Win32Api.CheckWin32Result(
                    Win32Api.UpdateLayeredWindow(_windowHandle, IntPtr.Zero, ref point, ref size,
                        _deviceContext, ref srcPoint, 0, ref blendFunc, Win32Constants.ULW_ALPHA),
                    "UpdateLayeredWindow");

                _logService.LogDebug("分层窗口已更新");
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新分层窗口失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 使窗口无效化，触发重绘
        /// </summary>
        public void Invalidate()
        {
            try
            {
                if (IsCreated)
                {
                    Win32Api.CheckWin32Result(
                        Win32Api.InvalidateRect(_windowHandle, IntPtr.Zero, true),
                        "InvalidateRect");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"使窗口无效化失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 注册窗口类
        /// </summary>
        private bool RegisterWindowClass()
        {
            try
            {
                // 这里可以注册自定义窗口类，如果不需要可以返回true
                // 对于透明窗口，通常可以使用预定义的窗口类
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"注册窗口类失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建窗口
        /// </summary>
        private bool CreateWindow()
        {
            try
            {
                uint exStyle = Win32Constants.WS_EX_LAYERED | Win32Constants.WS_EX_TOOLWINDOW | Win32Constants.WS_EX_NOACTIVATE;
                if (IsClickThrough)
                {
                    exStyle |= Win32Constants.WS_EX_TRANSPARENT;
                }

                uint style = Win32Constants.WS_POPUP | Win32Constants.WS_VISIBLE;

                _windowHandle = Win32Api.CreateWindowEx(
                    exStyle,
                    "STATIC", // 使用预定义的静态窗口类
                    "",
                    style,
                    Position.X, Position.Y, Size.Width, Size.Height,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                Win32Api.CheckWin32Handle(_windowHandle, "CreateWindowEx");

                // 设置透明度
                SetAlpha(Alpha);

                _logService.LogInfo($"透明窗口创建成功，句柄: {_windowHandle}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建窗口失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建绘图资源
        /// </summary>
        private void CreateDrawingResources()
        {
            try
            {
                ReleaseDrawingResources();

                _deviceContext = Win32Api.GetDC(_windowHandle);
                Win32Api.CheckWin32Handle(_deviceContext, "GetDC");

                _bitmapHandle = Win32Api.CreateCompatibleBitmap(_deviceContext, Size.Width, Size.Height);
                Win32Api.CheckWin32Handle(_bitmapHandle, "CreateCompatibleBitmap");

                var compatibleDC = Win32Api.CreateCompatibleDC(_deviceContext);
                Win32Api.CheckWin32Handle(compatibleDC, "CreateCompatibleDC");

                _oldBitmapHandle = Win32Api.SelectObject(compatibleDC, _bitmapHandle);
                Win32Api.ReleaseDC(_windowHandle, _deviceContext);
                _deviceContext = compatibleDC;

                _logService.LogDebug("绘图资源创建成功");
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建绘图资源失败: {ex.Message}", ex);
                ReleaseDrawingResources();
            }
        }

        /// <summary>
        /// 重新创建绘图资源
        /// </summary>
        private void RecreateDrawingResources()
        {
            ReleaseDrawingResources();
            CreateDrawingResources();
        }

        /// <summary>
        /// 释放绘图资源
        /// </summary>
        private void ReleaseDrawingResources()
        {
            try
            {
                if (_oldBitmapHandle != IntPtr.Zero && _deviceContext != IntPtr.Zero)
                {
                    Win32Api.SelectObject(_deviceContext, _oldBitmapHandle);
                    _oldBitmapHandle = IntPtr.Zero;
                }

                if (_bitmapHandle != IntPtr.Zero)
                {
                    Win32Api.DeleteObject(_bitmapHandle);
                    _bitmapHandle = IntPtr.Zero;
                }

                if (_deviceContext != IntPtr.Zero)
                {
                    Win32Api.DeleteDC(_deviceContext);
                    _deviceContext = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"释放绘图资源失败: {ex.Message}", ex);
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
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _logService.LogInfo("TransparentWindow 正在释放托管资源");
                }

                // 释放非托管资源
                Destroy();
                ReleaseDrawingResources();

                _isDisposed = true;
                _logService.LogInfo("TransparentWindow 资源释放完成");
            }
        }

        #endregion
    }
}