using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 叠加窗口管理器
    /// 提供透明窗口预测线绘制的完整UI控制功能
    /// </summary>
    public partial class OverlayManager : UserControl
    {
        #region 私有字段

        private readonly IDrawingService _drawingService;
        private readonly Dictionary<string, PredictionLine> _predictionLines;
        private bool _isInitialized;
        private IntPtr _targetWindowHandle;
        private DrawingConfiguration _currentConfiguration;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化OverlayManager
        /// </summary>
        /// <param name="drawingService">绘制服务</param>
        public OverlayManager(IDrawingService drawingService)
        {
            InitializeComponent();

            _drawingService = drawingService ?? throw new ArgumentNullException(nameof(drawingService));
            _predictionLines = new Dictionary<string, PredictionLine>();
            _currentConfiguration = DrawingConfiguration.CreateDefault();

            // 订阅绘制服务事件
            SubscribeToDrawingServiceEvents();

            _isInitialized = true;
        }

        /// <summary>
        /// 无参构造函数（用于设计器）
        /// </summary>
        public OverlayManager()
        {
            InitializeComponent();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 绘制状态变化事件
        /// </summary>
        public event EventHandler<OverlayManagerStatusEventArgs> StatusChanged;

        /// <summary>
        /// 预测线更新事件
        /// </summary>
        public event EventHandler<PredictionLinesUpdatedEventArgs> PredictionLinesUpdated;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<OverlayManagerErrorEventArgs> ErrorOccurred;

        #endregion

        #region 属性

        /// <summary>
        /// 是否正在绘制
        /// </summary>
        public bool IsDrawing => _drawingService?.IsDrawing ?? false;

        /// <summary>
        /// 是否启用窗口跟随
        /// </summary>
        public bool IsWindowFollowingEnabled => _drawingService?.IsWindowFollowingEnabled ?? false;

        /// <summary>
        /// 当前目标窗口句柄
        /// </summary>
        public IntPtr TargetWindowHandle => _targetWindowHandle;

        /// <summary>
        /// 当前预测线数量
        /// </summary>
        public int PredictionLineCount => _predictionLines.Count;

        /// <summary>
        /// 当前配置
        /// </summary>
        public DrawingConfiguration CurrentConfiguration => _currentConfiguration;

        /// <summary>
        /// 服务状态
        /// </summary>
        public OverlayManagerStatus Status => GetManagerStatus();

        #endregion

        #region 公共方法 - 窗口管理

        /// <summary>
        /// 开始在指定窗口上绘制
        /// </summary>
        /// <param name="targetWindowHandle">目标窗口句柄</param>
        /// <param name="predictionLines">预测线集合</param>
        /// <param name="config">绘制配置</param>
        /// <returns>是否成功开始绘制</returns>
        public async Task<bool> StartDrawingAsync(IntPtr targetWindowHandle, IEnumerable<PredictionLine> predictionLines = null, DrawingConfiguration config = null)
        {
            try
            {
                if (!_isInitialized)
                {
                    RaiseError("OverlayManager 未初始化");
                    return false;
                }

                if (_drawingService == null)
                {
                    RaiseError("绘制服务不可用");
                    return false;
                }

                if (targetWindowHandle == IntPtr.Zero)
                {
                    RaiseError("目标窗口句柄无效");
                    return false;
                }

                OnStatusChanged(OverlayManagerStatus.Starting, "开始绘制...");

                // 更新配置
                if (config != null)
                {
                    _currentConfiguration = config;
                    _drawingService.UpdateConfiguration(config);
                }

                // 更新预测线
                if (predictionLines != null)
                {
                    await UpdatePredictionLinesAsync(predictionLines);
                }

                // 开始绘制
                var success = _drawingService.StartDrawing(targetWindowHandle, _predictionLines.Values, _currentConfiguration);

                if (success)
                {
                    _targetWindowHandle = targetWindowHandle;
                    OnStatusChanged(OverlayManagerStatus.Drawing, "绘制中");
                }
                else
                {
                    OnStatusChanged(OverlayManagerStatus.Error, "开始绘制失败");
                }

                return success;
            }
            catch (Exception ex)
            {
                RaiseError($"开始绘制失败: {ex.Message}", ex);
                OnStatusChanged(OverlayManagerStatus.Error, "绘制出错");
                return false;
            }
        }

        /// <summary>
        /// 停止绘制
        /// </summary>
        /// <returns>是否成功停止绘制</returns>
        public async Task<bool> StopDrawingAsync()
        {
            try
            {
                if (!_isInitialized || _drawingService == null)
                {
                    return false;
                }

                OnStatusChanged(OverlayManagerStatus.Stopping, "停止绘制...");

                var success = _drawingService.StopDrawing();

                if (success)
                {
                    _targetWindowHandle = IntPtr.Zero;
                    OnStatusChanged(OverlayManagerStatus.Stopped, "绘制已停止");
                }
                else
                {
                    OnStatusChanged(OverlayManagerStatus.Error, "停止绘制失败");
                }

                return success;
            }
            catch (Exception ex)
            {
                RaiseError($"停止绘制失败: {ex.Message}", ex);
                OnStatusChanged(OverlayManagerStatus.Error, "停止出错");
                return false;
            }
        }

        /// <summary>
        /// 暂停绘制
        /// </summary>
        public void PauseDrawing()
        {
            try
            {
                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.PauseDrawing();
                    OnStatusChanged(OverlayManagerStatus.Paused, "绘制已暂停");
                }
            }
            catch (Exception ex)
            {
                RaiseError($"暂停绘制失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 恢复绘制
        /// </summary>
        public void ResumeDrawing()
        {
            try
            {
                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.ResumeDrawing();
                    OnStatusChanged(OverlayManagerStatus.Drawing, "绘制已恢复");
                }
            }
            catch (Exception ex)
            {
                RaiseError($"恢复绘制失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新目标窗口
        /// </summary>
        /// <param name="newTargetWindowHandle">新的目标窗口句柄</param>
        /// <returns>是否成功更新</returns>
        public async Task<bool> UpdateTargetWindowAsync(IntPtr newTargetWindowHandle)
        {
            try
            {
                if (!_isInitialized || _drawingService == null)
                {
                    return false;
                }

                var success = _drawingService.UpdateTargetWindow(newTargetWindowHandle);

                if (success)
                {
                    _targetWindowHandle = newTargetWindowHandle;
                    OnStatusChanged(Status, "目标窗口已更新");
                }

                return success;
            }
            catch (Exception ex)
            {
                RaiseError($"更新目标窗口失败: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region 公共方法 - 预测线管理

        /// <summary>
        /// 更新预测线集合
        /// </summary>
        /// <param name="predictionLines">新的预测线集合</param>
        /// <returns>是否成功更新</returns>
        public async Task<bool> UpdatePredictionLinesAsync(IEnumerable<PredictionLine> predictionLines)
        {
            try
            {
                if (predictionLines == null)
                {
                    return false;
                }

                var linesList = predictionLines.ToList();

                // 更新本地预测线集合
                _predictionLines.Clear();
                foreach (var line in linesList.Where(l => l != null && !string.IsNullOrWhiteSpace(l.Name)))
                {
                    _predictionLines[line.Name] = line;
                }

                // 更新绘制服务中的预测线
                if (_isInitialized && _drawingService != null)
                {
                    var success = _drawingService.UpdatePredictionLines(_predictionLines.Values);

                    if (success)
                    {
                        OnPredictionLinesUpdated(PredictionLineUpdateType.BatchUpdate, linesList.Count, "批量更新预测线");
                    }

                    return success;
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"更新预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 添加单条预测线
        /// </summary>
        /// <param name="predictionLine">预测线</param>
        /// <returns>是否成功添加</returns>
        public async Task<bool> AddPredictionLineAsync(PredictionLine predictionLine)
        {
            try
            {
                if (predictionLine == null || string.IsNullOrWhiteSpace(predictionLine.Name))
                {
                    return false;
                }

                // 添加到本地集合
                _predictionLines[predictionLine.Name] = predictionLine;

                // 添加到绘制服务
                if (_isInitialized && _drawingService != null)
                {
                    var success = _drawingService.AddPredictionLine(predictionLine);

                    if (success)
                    {
                        OnPredictionLinesUpdated(PredictionLineUpdateType.AddSingle, _predictionLines.Count, $"添加预测线: {predictionLine.Name}");
                    }

                    return success;
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"添加预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 移除预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        /// <returns>是否成功移除</returns>
        public async Task<bool> RemovePredictionLineAsync(string lineName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lineName) || !_predictionLines.ContainsKey(lineName))
                {
                    return false;
                }

                // 从本地集合移除
                _predictionLines.Remove(lineName);

                // 从绘制服务移除
                if (_isInitialized && _drawingService != null)
                {
                    var success = _drawingService.RemovePredictionLine(lineName);

                    if (success)
                    {
                        OnPredictionLinesUpdated(PredictionLineUpdateType.RemoveSingle, _predictionLines.Count, $"移除预测线: {lineName}");
                    }

                    return success;
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"移除预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 清空所有预测线
        /// </summary>
        public async Task ClearPredictionLinesAsync()
        {
            try
            {
                var previousCount = _predictionLines.Count;

                // 清空本地集合
                _predictionLines.Clear();

                // 清空绘制服务中的预测线
                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.ClearPredictionLines();
                    OnPredictionLinesUpdated(PredictionLineUpdateType.ClearAll, 0, "清空所有预测线");
                }
            }
            catch (Exception ex)
            {
                RaiseError($"清空预测线失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有预测线
        /// </summary>
        /// <returns>预测线集合</returns>
        public IReadOnlyList<PredictionLine> GetPredictionLines()
        {
            return _predictionLines.Values.ToList();
        }

        /// <summary>
        /// 根据名称获取预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        /// <returns>预测线，不存在返回null</returns>
        public PredictionLine GetPredictionLine(string lineName)
        {
            return _predictionLines.TryGetValue(lineName, out var line) ? line : null;
        }

        #endregion

        #region 公共方法 - 配置管理

        /// <summary>
        /// 更新绘制配置
        /// </summary>
        /// <param name="config">新的配置</param>
        public void UpdateConfiguration(DrawingConfiguration config)
        {
            try
            {
                _currentConfiguration = config ?? DrawingConfiguration.CreateDefault();

                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.UpdateConfiguration(_currentConfiguration);
                    OnStatusChanged(Status, "配置已更新");
                }
            }
            catch (Exception ex)
            {
                RaiseError($"更新配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置线条透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        public void SetLineOpacity(float opacity)
        {
            try
            {
                _currentConfiguration.LineOpacity = Math.Max(0, Math.Min(1, opacity));

                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.SetLineOpacity(_currentConfiguration.LineOpacity);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"设置线条透明度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置标签透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        public void SetLabelOpacity(float opacity)
        {
            try
            {
                _currentConfiguration.LabelOpacity = Math.Max(0, Math.Min(1, opacity));

                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.SetLabelOpacity(_currentConfiguration.LabelOpacity);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"设置标签透明度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="alpha">透明度 (0-255)</param>
        public void SetWindowAlpha(byte alpha)
        {
            try
            {
                _currentConfiguration.WindowAlpha = alpha;

                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.SetWindowAlpha(alpha);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"设置窗口透明度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 启用/禁用窗口跟随
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetWindowFollowing(bool enabled)
        {
            try
            {
                _currentConfiguration.EnableWindowFollowing = enabled;

                if (_isInitialized && _drawingService != null)
                {
                    _drawingService.SetWindowFollowing(enabled);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"设置窗口跟随失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 公共方法 - 状态查询

        /// <summary>
        /// 获取服务状态信息
        /// </summary>
        /// <returns>状态信息字典</returns>
        public Dictionary<string, object> GetServiceStatus()
        {
            try
            {
                var status = new Dictionary<string, object>
                {
                    ["OverlayManagerStatus"] = Status.ToString(),
                    ["IsInitialized"] = _isInitialized,
                    ["TargetWindowHandle"] = _targetWindowHandle.ToString(),
                    ["PredictionLineCount"] = _predictionLines.Count,
                    ["HasDrawingService"] = _drawingService != null
                };

                if (_drawingService != null)
                {
                    var drawingServiceStatus = _drawingService.GetServiceStatus();
                    foreach (var kvp in drawingServiceStatus)
                    {
                        status[$"DrawingService_{kvp.Key}"] = kvp.Value;
                    }

                    status["DrawingPerformance"] = _drawingService.GetPerformanceStats();
                }

                return status;
            }
            catch (Exception ex)
            {
                RaiseError($"获取服务状态失败: {ex.Message}", ex);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 检查管理器是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public bool IsAvailable()
        {
            return _isInitialized && _drawingService != null && _drawingService.IsServiceAvailable();
        }

        #endregion

        #region 私有方法 - 事件处理

        /// <summary>
        /// 订阅绘制服务事件
        /// </summary>
        private void SubscribeToDrawingServiceEvents()
        {
            try
            {
                if (_drawingService != null)
                {
                    _drawingService.DrawingStateChanged += OnDrawingServiceDrawingStateChanged;
                    _drawingService.WindowFollowingStateChanged += OnDrawingServiceWindowFollowingStateChanged;
                    _drawingService.PredictionLinesUpdated += OnDrawingServicePredictionLinesUpdated;
                }
            }
            catch (Exception ex)
            {
                RaiseError($"订阅绘制服务事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制服务绘制状态变化处理
        /// </summary>
        private void OnDrawingServiceDrawingStateChanged(object sender, DrawingStateChangedEventArgs e)
        {
            try
            {
                var newStatus = e.IsDrawing ? OverlayManagerStatus.Drawing : OverlayManagerStatus.Stopped;
                OnStatusChanged(newStatus, e.StatusDescription);
            }
            catch (Exception ex)
            {
                RaiseError($"处理绘制状态变化事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制服务窗口跟随状态变化处理
        /// </summary>
        private void OnDrawingServiceWindowFollowingStateChanged(object sender, WindowFollowingStateChangedEventArgs e)
        {
            try
            {
                OnStatusChanged(Status, e.StatusDescription);
            }
            catch (Exception ex)
            {
                RaiseError($"处理窗口跟随状态变化事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制服务预测线更新处理
        /// </summary>
        private void OnDrawingServicePredictionLinesUpdated(object sender, PredictionLinesUpdatedEventArgs e)
        {
            try
            {
                OnPredictionLinesUpdated(e.UpdateType, e.CurrentCount, e.UpdateDescription);
            }
            catch (Exception ex)
            {
                RaiseError($"处理预测线更新事件失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有方法 - 事件触发

        /// <summary>
        /// 触发状态变化事件
        /// </summary>
        protected virtual void OnStatusChanged(OverlayManagerStatus status, string description)
        {
            try
            {
                StatusChanged?.Invoke(this, new OverlayManagerStatusEventArgs
                {
                    Status = status,
                    Description = description,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // 避免在事件处理中引发异常
                System.Diagnostics.Debug.WriteLine($"触发状态变化事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发预测线更新事件
        /// </summary>
        protected virtual void OnPredictionLinesUpdated(PredictionLineUpdateType updateType, int lineCount, string description)
        {
            try
            {
                PredictionLinesUpdated?.Invoke(this, new PredictionLinesUpdatedEventArgs
                {
                    UpdateType = updateType,
                    LineCount = lineCount,
                    Description = description,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"触发预测线更新事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发错误事件
        /// </summary>
        protected virtual void RaiseError(string message, Exception exception = null)
        {
            try
            {
                ErrorOccurred?.Invoke(this, new OverlayManagerErrorEventArgs
                {
                    Message = message,
                    Exception = exception,
                    Timestamp = DateTime.Now
                });
            }
            catch
            {
                // 避免在错误处理中引发异常
            }
        }

        /// <summary>
        /// 获取管理器状态
        /// </summary>
        private OverlayManagerStatus GetManagerStatus()
        {
            if (!_isInitialized)
                return OverlayManagerStatus.Uninitialized;

            if (_drawingService == null)
                return OverlayManagerStatus.Error;

            if (_drawingService.IsDrawing)
                return OverlayManagerStatus.Drawing;

            if (_targetWindowHandle != IntPtr.Zero)
                return OverlayManagerStatus.Ready;

            return OverlayManagerStatus.Idle;
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // 停止绘制
                    StopDrawingAsync().Wait(1000);

                    // 取消订阅事件
                    if (_drawingService != null)
                    {
                        _drawingService.DrawingStateChanged -= OnDrawingServiceDrawingStateChanged;
                        _drawingService.WindowFollowingStateChanged -= OnDrawingServiceWindowFollowingStateChanged;
                        _drawingService.PredictionLinesUpdated -= OnDrawingServicePredictionLinesUpdated;
                    }

                    // 清理数据
                    _predictionLines.Clear();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放OverlayManager资源时发生错误: {ex.Message}");
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }

    #region 枚举和事件参数

    /// <summary>
    /// 叠加管理器状态
    /// </summary>
    public enum OverlayManagerStatus
    {
        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized,

        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 就绪
        /// </summary>
        Ready,

        /// <summary>
        /// 开始中
        /// </summary>
        Starting,

        /// <summary>
        /// 绘制中
        /// </summary>
        Drawing,

        /// <summary>
        /// 暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 停止中
        /// </summary>
        Stopping,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }

    /// <summary>
    /// 叠加管理器状态事件参数
    /// </summary>
    public class OverlayManagerStatusEventArgs : EventArgs
    {
        public OverlayManagerStatus Status { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 叠加管理器预测线更新事件参数
    /// </summary>
    public class OverlayManagerPredictionLinesUpdatedEventArgs : EventArgs
    {
        public PredictionLineUpdateType UpdateType { get; set; }
        public int LineCount { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 叠加管理器错误事件参数
    /// </summary>
    public class OverlayManagerErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}