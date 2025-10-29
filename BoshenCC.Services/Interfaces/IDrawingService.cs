using System;
using System.Collections.Generic;
using System.Drawing;
using BoshenCC.Models;

namespace BoshenCC.Services.Interfaces
{
    /// <summary>
    /// 绘制服务接口
    /// 提供透明窗口绘制和预测线管理的完整功能
    /// </summary>
    public interface IDrawingService : IDisposable
    {
        #region 事件

        /// <summary>
        /// 绘制状态变化事件
        /// </summary>
        event EventHandler<DrawingStateChangedEventArgs> DrawingStateChanged;

        /// <summary>
        /// 窗口跟随状态变化事件
        /// </summary>
        event EventHandler<WindowFollowingStateChangedEventArgs> WindowFollowingStateChanged;

        /// <summary>
        /// 预测线更新事件
        /// </summary>
        event EventHandler<PredictionLinesUpdatedEventArgs> PredictionLinesUpdated;

        #endregion

        #region 属性

        /// <summary>
        /// 是否正在绘制
        /// </summary>
        bool IsDrawing { get; }

        /// <summary>
        /// 是否启用窗口跟随
        /// </summary>
        bool IsWindowFollowingEnabled { get; }

        /// <summary>
        /// 当前目标窗口句柄
        /// </summary>
        IntPtr TargetWindowHandle { get; }

        /// <summary>
        /// 当前预测线集合
        /// </summary>
        IReadOnlyList<PredictionLine> CurrentPredictionLines { get; }

        /// <summary>
        /// 绘制配置
        /// </summary>
        DrawingConfiguration Configuration { get; }

        #endregion

        #region 窗口管理方法

        /// <summary>
        /// 开始在指定窗口上绘制
        /// </summary>
        /// <param name="targetWindowHandle">目标窗口句柄</param>
        /// <param name="predictionLines">预测线集合</param>
        /// <param name="config">绘制配置</param>
        /// <returns>是否成功开始绘制</returns>
        bool StartDrawing(IntPtr targetWindowHandle, IEnumerable<PredictionLine> predictionLines, DrawingConfiguration config = null);

        /// <summary>
        /// 停止绘制
        /// </summary>
        /// <returns>是否成功停止绘制</returns>
        bool StopDrawing();

        /// <summary>
        /// 暂停绘制
        /// </summary>
        void PauseDrawing();

        /// <summary>
        /// 恢复绘制
        /// </summary>
        void ResumeDrawing();

        /// <summary>
        /// 更新目标窗口
        /// </summary>
        /// <param name="newTargetWindowHandle">新的目标窗口句柄</param>
        /// <returns>是否成功更新</returns>
        bool UpdateTargetWindow(IntPtr newTargetWindowHandle);

        #endregion

        #region 预测线管理方法

        /// <summary>
        /// 更新预测线集合
        /// </summary>
        /// <param name="predictionLines">新的预测线集合</param>
        /// <returns>是否成功更新</returns>
        bool UpdatePredictionLines(IEnumerable<PredictionLine> predictionLines);

        /// <summary>
        /// 添加单条预测线
        /// </summary>
        /// <param name="predictionLine">预测线</param>
        /// <returns>是否成功添加</returns>
        bool AddPredictionLine(PredictionLine predictionLine);

        /// <summary>
        /// 移除预测线
        /// </summary>
        /// <param name="lineName">预测线名称</param>
        /// <returns>是否成功移除</returns>
        bool RemovePredictionLine(string lineName);

        /// <summary>
        /// 清空所有预测线
        /// </summary>
        void ClearPredictionLines();

        /// <summary>
        /// 刷新当前绘制
        /// </summary>
        /// <returns>是否成功刷新</returns>
        bool RefreshDrawing();

        #endregion

        #region 配置管理方法

        /// <summary>
        /// 更新绘制配置
        /// </summary>
        /// <param name="config">新的配置</param>
        void UpdateConfiguration(DrawingConfiguration config);

        /// <summary>
        /// 设置线条透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        void SetLineOpacity(float opacity);

        /// <summary>
        /// 设置标签透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        void SetLabelOpacity(float opacity);

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="alpha">透明度 (0-255)</param>
        void SetWindowAlpha(byte alpha);

        /// <summary>
        /// 启用/禁用窗口跟随
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetWindowFollowing(bool enabled);

        #endregion

        #region 状态查询方法

        /// <summary>
        /// 获取服务状态信息
        /// </summary>
        /// <returns>状态信息字典</returns>
        Dictionary<string, object> GetServiceStatus();

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        /// <returns>服务是否可用</returns>
        bool IsServiceAvailable();

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计</returns>
        DrawingPerformanceStats GetPerformanceStats();

        #endregion
    }

    #region 配置类

    /// <summary>
    /// 绘制配置类
    /// </summary>
    public class DrawingConfiguration
    {
        /// <summary>
        /// 窗口透明度 (0-255)
        /// </summary>
        public byte WindowAlpha { get; set; } = 200;

        /// <summary>
        /// 线条透明度 (0-1)
        /// </summary>
        public float LineOpacity { get; set; } = 0.8f;

        /// <summary>
        /// 标签透明度 (0-1)
        /// </summary>
        public float LabelOpacity { get; set; } = 0.9f;

        /// <summary>
        /// 是否启用抗锯齿
        /// </summary>
        public bool EnableAntiAliasing { get; set; } = true;

        /// <summary>
        /// 是否显示价格标签
        /// </summary>
        public bool ShowPriceLabels { get; set; } = true;

        /// <summary>
        /// 是否启用窗口跟随
        /// </summary>
        public bool EnableWindowFollowing { get; set; } = true;

        /// <summary>
        /// 跟随更新间隔（毫秒）
        /// </summary>
        public int FollowingUpdateInterval { get; set; } = 50;

        /// <summary>
        /// 绘制刷新率（FPS）
        /// </summary>
        public int RefreshRate { get; set; } = 30;

        /// <summary>
        /// 默认标签位置
        /// </summary>
        public string DefaultLabelPosition { get; set; } = "MiddleRight";

        /// <summary>
        /// 当前价格
        /// </summary>
        public double CurrentPrice { get; set; } = 0;

        /// <summary>
        /// 是否显示网格
        /// </summary>
        public bool ShowGrid { get; set; } = false;

        /// <summary>
        /// 网格颜色
        /// </summary>
        public Color GridColor { get; set; } = Color.FromArgb(200, 200, 200);

        /// <summary>
        /// 背景颜色
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static DrawingConfiguration CreateDefault()
        {
            return new DrawingConfiguration();
        }

        /// <summary>
        /// 创建高性能配置
        /// </summary>
        /// <returns>高性能配置</returns>
        public static DrawingConfiguration CreateHighPerformance()
        {
            return new DrawingConfiguration
            {
                EnableAntiAliasing = false,
                ShowPriceLabels = false,
                RefreshRate = 60,
                FollowingUpdateInterval = 16
            };
        }

        /// <summary>
        /// 创建高质量配置
        /// </summary>
        /// <returns>高质量配置</returns>
        public static DrawingConfiguration CreateHighQuality()
        {
            return new DrawingConfiguration
            {
                WindowAlpha = 180,
                LineOpacity = 0.9f,
                LabelOpacity = 0.95f,
                EnableAntiAliasing = true,
                ShowPriceLabels = true,
                ShowGrid = true
            };
        }
    }

    /// <summary>
    /// 绘制性能统计
    /// </summary>
    public class DrawingPerformanceStats
    {
        /// <summary>
        /// 当前FPS
        /// </summary>
        public double CurrentFPS { get; set; }

        /// <summary>
        /// 平均FPS
        /// </summary>
        public double AverageFPS { get; set; }

        /// <summary>
        /// 绘制次数
        /// </summary>
        public long DrawCount { get; set; }

        /// <summary>
        /// 跟随更新次数
        /// </summary>
        public long FollowingUpdateCount { get; set; }

        /// <summary>
        /// 总绘制时间（毫秒）
        /// </summary>
        public double TotalDrawTime { get; set; }

        /// <summary>
        /// 平均绘制时间（毫秒）
        /// </summary>
        public double AverageDrawTime { get; set; }

        /// <summary>
        /// 内存使用量（字节）
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// 服务启动时间
        /// </summary>
        public DateTime ServiceStartTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }

    #endregion

    #region 事件参数类

    /// <summary>
    /// 绘制状态变化事件参数
    /// </summary>
    public class DrawingStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否正在绘制
        /// </summary>
        public bool IsDrawing { get; set; }

        /// <summary>
        /// 状态变化时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 状态描述
        /// </summary>
        public string StatusDescription { get; set; }
    }

    /// <summary>
    /// 窗口跟随状态变化事件参数
    /// </summary>
    public class WindowFollowingStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否启用跟随
        /// </summary>
        public bool IsFollowingEnabled { get; set; }

        /// <summary>
        /// 目标窗口句柄
        /// </summary>
        public IntPtr TargetWindowHandle { get; set; }

        /// <summary>
        /// 状态变化时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 状态描述
        /// </summary>
        public string StatusDescription { get; set; }
    }

    /// <summary>
    /// 预测线更新事件参数
    /// </summary>
    public class PredictionLinesUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// 更新前的预测线数量
        /// </summary>
        public int PreviousCount { get; set; }

        /// <summary>
        /// 更新后的预测线数量
        /// </summary>
        public int CurrentCount { get; set; }

        /// <summary>
        /// 更新类型
        /// </summary>
        public PredictionLineUpdateType UpdateType { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 更新描述
        /// </summary>
        public string UpdateDescription { get; set; }
    }

    /// <summary>
    /// 预测线更新类型
    /// </summary>
    public enum PredictionLineUpdateType
    {
        /// <summary>
        /// 批量更新
        /// </summary>
        BatchUpdate,

        /// <summary>
        /// 添加单条
        /// </summary>
        AddSingle,

        /// <summary>
        /// 移除单条
        /// </summary>
        RemoveSingle,

        /// <summary>
        /// 清空所有
        /// </summary>
        ClearAll,

        /// <summary>
        /// 刷新
        /// </summary>
        Refresh
    }

    #endregion
}