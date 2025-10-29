using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BoshenCC.Core.Utils;
using BoshenCC.Core.Services;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Services.Implementations
{
    /// <summary>
    /// 绘制服务实现
    /// 整合透明窗口、绘制引擎和窗口跟踪，提供完整的预测线绘制功能
    /// </summary>
    public class DrawingService : IDrawingService
    {
        #region 私有字段

        private readonly ILogService _logService;
        private readonly IWindowManagerService _windowManagerService;
        private readonly DrawingEngine _drawingEngine;
        private readonly WindowTracker _windowTracker;

        private readonly ConcurrentDictionary<string, PredictionLine> _predictionLines;
        private readonly object _stateLock = new object();
        private readonly Timer _drawingTimer;
        private readonly Stopwatch _performanceStopwatch;
        private readonly Queue<double> _fpsHistory;

        private DrawingConfiguration _configuration;
        private string _overlayWindowId;
        private IntPtr _targetWindowHandle;
        private bool _isDrawing;
        private bool _isPaused;
        private bool _isWindowFollowingEnabled;
        private bool _disposed;

        // 性能统计
        private long _drawCount;
        private long _followingUpdateCount;
        private double _totalDrawTime;
        private DateTime _serviceStartTime;

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化绘制服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="windowManagerService">窗口管理服务</param>
        public DrawingService(ILogService logService, IWindowManagerService windowManagerService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _windowManagerService = windowManagerService ?? throw new ArgumentNullException(nameof(windowManagerService));

            try
            {
                // 初始化组件
                _drawingEngine = new DrawingEngine(_logService);

                // 创建WindowTracker时需要ILogger<WindowTracker>，这里使用LogService包装
                var logger = new WindowTrackerLoggerAdapter(_logService);
                _windowTracker = new WindowTracker(logger);

                // 初始化集合和工具
                _predictionLines = new ConcurrentDictionary<string, PredictionLine>();
                _fpsHistory = new Queue<double>();
                _performanceStopwatch = new Stopwatch();

                // 初始化配置
                _configuration = DrawingConfiguration.CreateDefault();

                // 创建绘制定时器
                _drawingTimer = new Timer(OnDrawingTick, null, Timeout.Infinite, Timeout.Infinite);

                // 订阅窗口跟踪事件
                _windowTracker.WindowPositionChanged += OnTargetWindowPositionChanged;
                _windowTracker.WindowSizeChanged += OnTargetWindowSizeChanged;
                _windowTracker.WindowDestroyed += OnTargetWindowDestroyed;

                // 初始化性能统计
                _serviceStartTime = DateTime.Now;

                _logService.LogInfo("DrawingService 初始化完成");
            }
            catch (Exception ex)
            {
                _logService.LogError($"DrawingService 初始化失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~DrawingService()
        {
            Dispose(false);
        }

        #endregion

        #region 事件实现

        /// <summary>
        /// 绘制状态变化事件
        /// </summary>
        public event EventHandler<DrawingStateChangedEventArgs> DrawingStateChanged;

        /// <summary>
        /// 窗口跟随状态变化事件
        /// </summary>
        public event EventHandler<WindowFollowingStateChangedEventArgs> WindowFollowingStateChanged;

        /// <summary>
        /// 预测线更新事件
        /// </summary>
        public event EventHandler<PredictionLinesUpdatedEventArgs> PredictionLinesUpdated;

        #endregion

        #region 属性实现

        /// <summary>
        /// 是否正在绘制
        /// </summary>
        public bool IsDrawing => _isDrawing;

        /// <summary>
        /// 是否启用窗口跟随
        /// </summary>
        public bool IsWindowFollowingEnabled => _isWindowFollowingEnabled;

        /// <summary>
        /// 当前目标窗口句柄
        /// </summary>
        public IntPtr TargetWindowHandle => _targetWindowHandle;

        /// <summary>
        /// 当前预测线集合
        /// </summary>
        public IReadOnlyList<PredictionLine> CurrentPredictionLines => _predictionLines.Values.ToList();

        /// <summary>
        /// 绘制配置
        /// </summary>
        public DrawingConfiguration Configuration => _configuration;

        #endregion

        #region 窗口管理方法

        /// <summary>
        /// 开始在指定窗口上绘制
        /// </summary>
        /// <param name="targetWindowHandle">目标窗口句柄</param>
        /// <param name="predictionLines">预测线集合</param>
        /// <param name="config">绘制配置</param>
        /// <returns>是否成功开始绘制</returns>
        public bool StartDrawing(IntPtr targetWindowHandle, IEnumerable<PredictionLine> predictionLines, DrawingConfiguration config = null)
        {
            lock (_stateLock)
            {
                try
                {
                    if (_disposed)
                    {
                        _logService.LogWarning("DrawingService 已释放，无法开始绘制");
                        return false;
                    }

                    if (_isDrawing)
                    {
                        _logService.LogWarning("绘制已在进行中，先停止当前绘制");
                        StopDrawing();
                    }

                    if (targetWindowHandle == IntPtr.Zero)
                    {
                        _logService.LogError("目标窗口句柄无效");
                        return false;
                    }

                    _logService.LogInfo($"开始绘制，目标窗口句柄: {targetWindowHandle}");

                    // 更新配置
                    if (config != null)
                    {
                        _configuration = config;
                        ApplyConfigurationToEngine();
                    }

                    // 更新预测线
                    if (predictionLines != null)
                    {
                        UpdatePredictionLines(predictionLines);
                    }

                    // 设置目标窗口
                    _targetWindowHandle = targetWindowHandle;

                    // 开始跟踪目标窗口
                    if (_configuration.EnableWindowFollowing)
                    {
                        _windowTracker.TrackWindow(targetWindowHandle, "目标窗口");
                        _windowTracker.StartTracking();
                        _isWindowFollowingEnabled = true;
                    }

                    // 创建叠加窗口
                    if (!CreateOverlayWindow())
                    {
                        _logService.LogError("创建叠加窗口失败");
                        return false;
                    }

                    // 启动绘制定时器
                    var interval = 1000 / _configuration.RefreshRate;
                    _drawingTimer.Change(interval, interval);

                    // 更新状态
                    _isDrawing = true;
                    _isPaused = false;

                    // 触发状态变化事件
                    OnDrawingStateChanged(true, "开始绘制");
                    OnWindowFollowingStateChanged(_isWindowFollowingEnabled, "窗口跟随已启用");

                    _logService.LogInfo("绘制开始成功");
                    return true;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"开始绘制失败: {ex.Message}", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 停止绘制
        /// </summary>
        /// <returns>是否成功停止绘制</returns>
        public bool StopDrawing()
        {
            lock (_stateLock)
            {
                try
                {
                    if (!_isDrawing)
                    {
                        _logService.LogDebug("绘制未在进行中，无需停止");
                        return true;
                    }

                    _logService.LogInfo("开始停止绘制");

                    // 停止绘制定时器
                    _drawingTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    // 停止窗口跟踪
                    if (_isWindowFollowingEnabled)
                    {
                        _windowTracker.StopTracking();
                        _windowTracker.UntrackWindow(_targetWindowHandle);
                        _isWindowFollowingEnabled = false;
                    }

                    // 销毁叠加窗口
                    DestroyOverlayWindow();

                    // 更新状态
                    _isDrawing = false;
                    _isPaused = false;
                    _targetWindowHandle = IntPtr.Zero;

                    // 触发状态变化事件
                    OnDrawingStateChanged(false, "绘制已停止");
                    OnWindowFollowingStateChanged(false, "窗口跟随已禁用");

                    _logService.LogInfo("绘制停止成功");
                    return true;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"停止绘制失败: {ex.Message}", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 暂停绘制
        /// </summary>
        public void PauseDrawing()
        {
            lock (_stateLock)
            {
                if (_isDrawing && !_isPaused)
                {
                    _isPaused = true;
                    _drawingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    OnDrawingStateChanged(false, "绘制已暂停");
                    _logService.LogInfo("绘制已暂停");
                }
            }
        }

        /// <summary>
        /// 恢复绘制
        /// </summary>
        public void ResumeDrawing()
        {
            lock (_stateLock)
            {
                if (_isDrawing && _isPaused)
                {
                    _isPaused = false;
                    var interval = 1000 / _configuration.RefreshRate;
                    _drawingTimer.Change(interval, interval);
                    OnDrawingStateChanged(true, "绘制已恢复");
                    _logService.LogInfo("绘制已恢复");
                }
            }
        }

        /// <summary>
        /// 更新目标窗口
        /// </summary>
        /// <param name="newTargetWindowHandle">新的目标窗口句柄</param>
        /// <returns>是否成功更新</returns>
        public bool UpdateTargetWindow(IntPtr newTargetWindowHandle)
        {
            lock (_stateLock)
            {
                try
                {
                    if (newTargetWindowHandle == IntPtr.Zero)
                    {
                        _logService.LogError("新的目标窗口句柄无效");
                        return false;
                    }

                    if (_targetWindowHandle == newTargetWindowHandle)
                    {
                        _logService.LogDebug("目标窗口句柄未发生变化");
                        return true;
                    }

                    _logService.LogInfo($"更新目标窗口: {_targetWindowHandle} -> {newTargetWindowHandle}");

                    // 停止跟踪旧窗口
                    if (_isWindowFollowingEnabled && _targetWindowHandle != IntPtr.Zero)
                    {
                        _windowTracker.UntrackWindow(_targetWindowHandle);
                    }

                    // 更新目标窗口
                    var oldHandle = _targetWindowHandle;
                    _targetWindowHandle = newTargetWindowHandle;

                    // 开始跟踪新窗口
                    if (_isWindowFollowingEnabled)
                    {
                        _windowTracker.TrackWindow(newTargetWindowHandle, "目标窗口");
                    }

                    // 重新定位叠加窗口
                    RepositionOverlayWindow();

                    _logService.LogInfo("目标窗口更新成功");
                    return true;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"更新目标窗口失败: {ex.Message}", ex);
                    return false;
                }
            }
        }

        #endregion

        #region 预测线管理方法

        /// <summary>
        /// 更新预测线集合
        /// </summary>
        /// <param name="predictionLines">新的预测线集合</param>
        /// <returns>是否成功更新</returns>
        public bool UpdatePredictionLines(IEnumerable<PredictionLine> predictionLines)
        {
            try
            {
                if (predictionLines == null)
                {
                    _logService.LogWarning("预测线集合为空");
                    return false;
                }

                var linesList = predictionLines.ToList();
                var previousCount = _predictionLines.Count;

                // 清空现有预测线
                _predictionLines.Clear();

                // 添加新预测线
                foreach (var line in linesList)
                {
                    if (line != null && !string.IsNullOrWhiteSpace(line.Name))
                    {
                        _predictionLines.TryAdd(line.Name, line);
                    }
                }

                // 触发更新事件
                OnPredictionLinesUpdated(previousCount, _predictionLines.Count, PredictionLineUpdateType.BatchUpdate,
                    $"批量更新预测线：{previousCount} -> {_predictionLines.Count}");

                // 如果正在绘制，立即刷新
                if (_isDrawing && !_isPaused)
                {
                    Task.Run(() => RefreshDrawing());
                }

                _logService.LogInfo($"预测线更新成功，当前数量: {_predictionLines.Count}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 添加单条预测线
        /// </summary>
        /// <param name="predictionLine">预测线</param>
        /// <returns>是否成功添加</returns>
        public bool AddPredictionLine(PredictionLine predictionLine)
        {
            try
            {
                if (predictionLine == null || string.IsNullOrWhiteSpace(predictionLine.Name))
                {
                    _logService.LogWarning("预测线数据无效");
                    return false;
                }

                var previousCount = _predictionLines.Count;
                _predictionLines.TryAdd(predictionLine.Name, predictionLine);

                // 触发更新事件
                OnPredictionLinesUpdated(previousCount, _predictionLines.Count, PredictionLineUpdateType.AddSingle,
                    $"添加预测线: {predictionLine.Name}");

                // 如果正在绘制，立即刷新
                if (_isDrawing && !_isPaused)
                {
                    Task.Run(() => RefreshDrawing());
                }

                _logService.LogInfo($"预测线添加成功: {predictionLine.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"添加预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 移除预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemovePredictionLine(string lineName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lineName))
                {
                    _logService.LogWarning("预测线名称为空");
                    return false;
                }

                var previousCount = _predictionLines.Count;
                var removed = _predictionLines.TryRemove(lineName, out _);

                if (removed)
                {
                    // 触发更新事件
                    OnPredictionLinesUpdated(previousCount, _predictionLines.Count, PredictionLineUpdateType.RemoveSingle,
                        $"移除预测线: {lineName}");

                    // 如果正在绘制，立即刷新
                    if (_isDrawing && !_isPaused)
                    {
                        Task.Run(() => RefreshDrawing());
                    }

                    _logService.LogInfo($"预测线移除成功: {lineName}");
                }
                else
                {
                    _logService.LogWarning($"预测线不存在: {lineName}");
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logService.LogError($"移除预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 清空所有预测线
        /// </summary>
        public void ClearPredictionLines()
        {
            try
            {
                var previousCount = _predictionLines.Count;
                _predictionLines.Clear();

                // 触发更新事件
                OnPredictionLinesUpdated(previousCount, 0, PredictionLineUpdateType.ClearAll, "清空所有预测线");

                // 如果正在绘制，立即刷新
                if (_isDrawing && !_isPaused)
                {
                    Task.Run(() => RefreshDrawing());
                }

                _logService.LogInfo("所有预测线已清空");
            }
            catch (Exception ex)
            {
                _logService.LogError($"清空预测线失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 刷新当前绘制
        /// </summary>
        /// <returns>是否成功刷新</returns>
        public bool RefreshDrawing()
        {
            try
            {
                if (!_isDrawing || _isPaused || string.IsNullOrEmpty(_overlayWindowId))
                {
                    return false;
                }

                // 获取叠加窗口
                var overlayWindow = _windowManagerService.GetWindow(_overlayWindowId);
                if (overlayWindow == null || !overlayWindow.IsCreated)
                {
                    _logService.LogWarning("叠加窗口无效，无法刷新绘制");
                    return false;
                }

                // 执行绘制
                return PerformDrawing(overlayWindow);
            }
            catch (Exception ex)
            {
                _logService.LogError($"刷新绘制失败: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region 配置管理方法

        /// <summary>
        /// 更新绘制配置
        /// </summary>
        /// <param name="config">新的配置</param>
        public void UpdateConfiguration(DrawingConfiguration config)
        {
            try
            {
                _configuration = config ?? DrawingConfiguration.CreateDefault();
                ApplyConfigurationToEngine();

                // 如果正在绘制，应用窗口透明度
                if (_isDrawing && !string.IsNullOrEmpty(_overlayWindowId))
                {
                    _windowManagerService.SetWindowAlpha(_overlayWindowId, _configuration.WindowAlpha);
                }

                // 更新绘制定时器间隔
                if (_isDrawing && !_isPaused)
                {
                    var interval = 1000 / _configuration.RefreshRate;
                    _drawingTimer.Change(interval, interval);
                }

                _logService.LogInfo("绘制配置已更新");
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新绘制配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置线条透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        public void SetLineOpacity(float opacity)
        {
            _configuration.LineOpacity = Math.Max(0, Math.Min(1, opacity));
            _drawingEngine.SetLineOpacity(_configuration.LineOpacity);
            _logService.LogDebug($"线条透明度已更新: {_configuration.LineOpacity:F2}");
        }

        /// <summary>
        /// 设置标签透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        public void SetLabelOpacity(float opacity)
        {
            _configuration.LabelOpacity = Math.Max(0, Math.Min(1, opacity));
            _drawingEngine.SetLabelOpacity(_configuration.LabelOpacity);
            _logService.LogDebug($"标签透明度已更新: {_configuration.LabelOpacity:F2}");
        }

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="alpha">透明度 (0-255)</param>
        public void SetWindowAlpha(byte alpha)
        {
            _configuration.WindowAlpha = alpha;
            if (_isDrawing && !string.IsNullOrEmpty(_overlayWindowId))
            {
                _windowManagerService.SetWindowAlpha(_overlayWindowId, alpha);
            }
            _logService.LogDebug($"窗口透明度已更新: {alpha}");
        }

        /// <summary>
        /// 启用/禁用窗口跟随
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetWindowFollowing(bool enabled)
        {
            _configuration.EnableWindowFollowing = enabled;

            if (_isDrawing)
            {
                if (enabled && !_isWindowFollowingEnabled)
                {
                    // 启用跟随
                    _windowTracker.TrackWindow(_targetWindowHandle, "目标窗口");
                    _windowTracker.StartTracking();
                    _isWindowFollowingEnabled = true;
                    OnWindowFollowingStateChanged(true, "窗口跟随已启用");
                }
                else if (!enabled && _isWindowFollowingEnabled)
                {
                    // 禁用跟随
                    _windowTracker.StopTracking();
                    _windowTracker.UntrackWindow(_targetWindowHandle);
                    _isWindowFollowingEnabled = false;
                    OnWindowFollowingStateChanged(false, "窗口跟随已禁用");
                }
            }

            _logService.LogInfo($"窗口跟随已{(enabled ? "启用" : "禁用")}");
        }

        #endregion

        #region 状态查询方法

        /// <summary>
        /// 获取服务状态信息
        /// </summary>
        /// <returns>状态信息字典</returns>
        public Dictionary<string, object> GetServiceStatus()
        {
            try
            {
                return new Dictionary<string, object>
                {
                    ["IsDrawing"] = _isDrawing,
                    ["IsPaused"] = _isPaused,
                    ["IsWindowFollowingEnabled"] = _isWindowFollowingEnabled,
                    ["TargetWindowHandle"] = _targetWindowHandle.ToString(),
                    ["OverlayWindowId"] = _overlayWindowId ?? "None",
                    ["PredictionLineCount"] = _predictionLines.Count,
                    ["WindowAlpha"] = _configuration.WindowAlpha,
                    ["LineOpacity"] = _configuration.LineOpacity,
                    ["LabelOpacity"] = _configuration.LabelOpacity,
                    ["RefreshRate"] = _configuration.RefreshRate,
                    ["EnableAntiAliasing"] = _configuration.EnableAntiAliasing,
                    ["ShowPriceLabels"] = _configuration.ShowPriceLabels,
                    ["ServiceStartTime"] = _serviceStartTime,
                    ["IsDisposed"] = _disposed
                };
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取服务状态失败: {ex.Message}", ex);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        /// <returns>服务是否可用</returns>
        public bool IsServiceAvailable()
        {
            return !_disposed && _drawingEngine != null && _windowManagerService != null;
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计</returns>
        public DrawingPerformanceStats GetPerformanceStats()
        {
            try
            {
                var currentFps = _fpsHistory.Count > 0 ? _fpsHistory.Average() : 0;
                var avgDrawTime = _drawCount > 0 ? _totalDrawTime / _drawCount : 0;

                return new DrawingPerformanceStats
                {
                    CurrentFPS = currentFps,
                    AverageFPS = currentFps, // 简化实现，实际应该计算长期平均值
                    DrawCount = _drawCount,
                    FollowingUpdateCount = _followingUpdateCount,
                    TotalDrawTime = _totalDrawTime,
                    AverageDrawTime = avgDrawTime,
                    MemoryUsage = GC.GetTotalMemory(false),
                    ServiceStartTime = _serviceStartTime,
                    LastUpdateTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取性能统计失败: {ex.Message}", ex);
                return new DrawingPerformanceStats();
            }
        }

        #endregion

        #region 私有方法 - 窗口管理

        /// <summary>
        /// 创建叠加窗口
        /// </summary>
        /// <returns>是否创建成功</returns>
        private bool CreateOverlayWindow()
        {
            try
            {
                if (_targetWindowHandle == IntPtr.Zero)
                {
                    _logService.LogError("目标窗口句柄无效，无法创建叠加窗口");
                    return false;
                }

                // 获取目标窗口位置和尺寸
                if (!Win32Api.GetWindowRect(_targetWindowHandle, out WindowStructs.RECT targetRect))
                {
                    _logService.LogError("无法获取目标窗口位置信息");
                    return false;
                }

                // 创建叠加窗口
                _overlayWindowId = _windowManagerService.CreateWindow(
                    "预测线叠加窗口",
                    targetRect.Left,
                    targetRect.Top,
                    targetRect.Width,
                    targetRect.Height,
                    _configuration.WindowAlpha,
                    true // 点击穿透
                );

                if (string.IsNullOrEmpty(_overlayWindowId))
                {
                    _logService.LogError("创建叠加窗口失败");
                    return false;
                }

                // 显示窗口
                _windowManagerService.ShowWindow(_overlayWindowId);

                _logService.LogInfo($"叠加窗口创建成功: {_overlayWindowId}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建叠加窗口失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 销毁叠加窗口
        /// </summary>
        private void DestroyOverlayWindow()
        {
            try
            {
                if (!string.IsNullOrEmpty(_overlayWindowId))
                {
                    _windowManagerService.DestroyWindow(_overlayWindowId);
                    _overlayWindowId = null;
                    _logService.LogInfo("叠加窗口已销毁");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"销毁叠加窗口失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重新定位叠加窗口
        /// </summary>
        private void RepositionOverlayWindow()
        {
            try
            {
                if (string.IsNullOrEmpty(_overlayWindowId) || _targetWindowHandle == IntPtr.Zero)
                {
                    return;
                }

                // 获取目标窗口位置
                if (!Win32Api.GetWindowRect(_targetWindowHandle, out WindowStructs.RECT targetRect))
                {
                    return;
                }

                // 更新叠加窗口位置和尺寸
                _windowManagerService.SetWindowPosition(_overlayWindowId, targetRect.Left, targetRect.Top);
                _windowManagerService.SetWindowSize(_overlayWindowId, targetRect.Width, targetRect.Height);

                _logService.LogDebug($"叠加窗口位置已更新: ({targetRect.Left},{targetRect.Top}) {targetRect.Width}x{targetRect.Height}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"重新定位叠加窗口失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有方法 - 绘制

        /// <summary>
        /// 绘制定时器回调
        /// </summary>
        /// <param name="state">状态对象</param>
        private void OnDrawingTick(object state)
        {
            if (!_isDrawing || _isPaused || string.IsNullOrEmpty(_overlayWindowId))
            {
                return;
            }

            try
            {
                var overlayWindow = _windowManagerService.GetWindow(_overlayWindowId);
                if (overlayWindow != null && overlayWindow.IsCreated)
                {
                    PerformDrawing(overlayWindow);
                    UpdatePerformanceStats();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制定时器回调失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行绘制
        /// </summary>
        /// <param name="overlayWindow">叠加窗口</param>
        /// <returns>是否绘制成功</returns>
        private bool PerformDrawing(TransparentWindow overlayWindow)
        {
            _performanceStopwatch.Restart();

            try
            {
                // 获取设备上下文
                var hdc = overlayWindow.GetDeviceContext();
                if (hdc == IntPtr.Zero)
                {
                    return false;
                }

                // 创建Graphics对象
                using var graphics = Graphics.FromHdc(hdc);
                if (graphics == null)
                {
                    return false;
                }

                // 定义绘制区域
                var chartBounds = new RectangleF(0, 0, overlayWindow.Size.Width, overlayWindow.Size.Height);

                // 执行绘制
                var success = _drawingEngine.DrawPredictionLines(graphics, _predictionLines.Values, chartBounds);

                // 更新分层窗口
                if (success)
                {
                    overlayWindow.UpdateWindow();
                }

                // 更新统计
                _drawCount++;
                _totalDrawTime += _performanceStopwatch.Elapsed.TotalMilliseconds;

                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"执行绘制失败: {ex.Message}", ex);
                return false;
            }
            finally
            {
                _performanceStopwatch.Stop();
            }
        }

        /// <summary>
        /// 应用配置到绘制引擎
        /// </summary>
        private void ApplyConfigurationToEngine()
        {
            try
            {
                _drawingEngine.SetAntiAliasing(_configuration.EnableAntiAliasing);
                _drawingEngine.SetLineOpacity(_configuration.LineOpacity);
                _drawingEngine.SetLabelOpacity(_configuration.LabelOpacity);
                _drawingEngine.SetCurrentPrice(_configuration.CurrentPrice);
                _drawingEngine.SetPriceLabelsVisibility(_configuration.ShowPriceLabels);
                _drawingEngine.SetGridVisibility(_configuration.ShowGrid);

                // 更新绘制引擎配置
                var engineConfig = new DrawingEngine.DrawingConfig
                {
                    EnableAntiAliasing = _configuration.EnableAntiAliasing,
                    ShowGrid = _configuration.ShowGrid,
                    ShowPriceLabels = _configuration.ShowPriceLabels,
                    BackgroundColor = _configuration.BackgroundColor,
                    CurrentPrice = _configuration.CurrentPrice,
                    LineOpacity = _configuration.LineOpacity,
                    LabelOpacity = _configuration.LabelOpacity
                };

                _drawingEngine.UpdateConfig(engineConfig);

                _logService.LogDebug("配置已应用到绘制引擎");
            }
            catch (Exception ex)
            {
                _logService.LogError($"应用配置到绘制引擎失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新性能统计
        /// </summary>
        private void UpdatePerformanceStats()
        {
            try
            {
                var frameTime = _performanceStopwatch.Elapsed.TotalMilliseconds;
                if (frameTime > 0)
                {
                    var fps = 1000.0 / frameTime;
                    _fpsHistory.Enqueue(fps);

                    // 保持最近100帧的历史
                    while (_fpsHistory.Count > 100)
                    {
                        _fpsHistory.Dequeue();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新性能统计失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有方法 - 事件处理

        /// <summary>
        /// 目标窗口位置变化处理
        /// </summary>
        private void OnTargetWindowPositionChanged(object sender, WindowPositionChangedEventArgs e)
        {
            if (e.WindowHandle == _targetWindowHandle)
            {
                RepositionOverlayWindow();
                _followingUpdateCount++;
            }
        }

        /// <summary>
        /// 目标窗口大小变化处理
        /// </summary>
        private void OnTargetWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            if (e.WindowHandle == _targetWindowHandle)
            {
                RepositionOverlayWindow();
                _followingUpdateCount++;
            }
        }

        /// <summary>
        /// 目标窗口销毁处理
        /// </summary>
        private void OnTargetWindowDestroyed(object sender, WindowDestroyedEventArgs e)
        {
            if (e.WindowHandle == _targetWindowHandle)
            {
                _logService.LogWarning("目标窗口已销毁，停止绘制");
                StopDrawing();
            }
        }

        #endregion

        #region 私有方法 - 事件触发

        /// <summary>
        /// 触发绘制状态变化事件
        /// </summary>
        protected virtual void OnDrawingStateChanged(bool isDrawing, string description)
        {
            try
            {
                DrawingStateChanged?.Invoke(this, new DrawingStateChangedEventArgs
                {
                    IsDrawing = isDrawing,
                    Timestamp = DateTime.Now,
                    StatusDescription = description
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发绘制状态变化事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 触发窗口跟随状态变化事件
        /// </summary>
        protected virtual void OnWindowFollowingStateChanged(bool enabled, string description)
        {
            try
            {
                WindowFollowingStateChanged?.Invoke(this, new WindowFollowingStateChangedEventArgs
                {
                    IsFollowingEnabled = enabled,
                    TargetWindowHandle = _targetWindowHandle,
                    Timestamp = DateTime.Now,
                    StatusDescription = description
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发窗口跟随状态变化事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 触发预测线更新事件
        /// </summary>
        protected virtual void OnPredictionLinesUpdated(int previousCount, int currentCount, PredictionLineUpdateType updateType, string description)
        {
            try
            {
                PredictionLinesUpdated?.Invoke(this, new PredictionLinesUpdatedEventArgs
                {
                    PreviousCount = previousCount,
                    CurrentCount = currentCount,
                    UpdateType = updateType,
                    Timestamp = DateTime.Now,
                    UpdateDescription = description
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"触发预测线更新事件失败: {ex.Message}", ex);
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
                    _logService.LogInfo("DrawingService 正在释放托管资源");
                }

                // 停止绘制
                StopDrawing();

                // 释放定时器
                _drawingTimer?.Dispose();

                // 释放组件
                _drawingEngine?.Dispose();
                _windowTracker?.Dispose();

                // 清理事件
                DrawingStateChanged = null;
                WindowFollowingStateChanged = null;
                PredictionLinesUpdated = null;

                // 清理数据
                _predictionLines.Clear();
                _fpsHistory.Clear();

                _disposed = true;
                _logService.LogInfo("DrawingService 资源释放完成");
            }
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// WindowTracker日志适配器
    /// 将ILogService适配为ILogger<WindowTracker>
    /// </summary>
    internal class WindowTrackerLoggerAdapter : Microsoft.Extensions.Logging.ILogger
    {
        private readonly ILogService _logService;

        public WindowTrackerLoggerAdapter(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true; // 简化实现，总是启用
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                var message = formatter(state, exception);
                switch (logLevel)
                {
                    case Microsoft.Extensions.Logging.LogLevel.Debug:
                        _logService.LogDebug(message);
                        break;
                    case Microsoft.Extensions.Logging.LogLevel.Information:
                        _logService.LogInfo(message);
                        break;
                    case Microsoft.Extensions.Logging.LogLevel.Warning:
                        _logService.LogWarning(message);
                        break;
                    case Microsoft.Extensions.Logging.LogLevel.Error:
                        _logService.LogError(message, exception);
                        break;
                    case Microsoft.Extensions.Logging.LogLevel.Critical:
                        _logService.LogError($"CRITICAL: {message}", exception);
                        break;
                    default:
                        _logService.LogInfo(message);
                        break;
                }
            }
            catch
            {
                // 忽略日志记录错误
            }
        }
    }

    #endregion
}