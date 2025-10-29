using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 绘图引擎
    /// 核心绘制管理器，协调线条渲染器和价格标签渲染器，提供统一的绘制接口
    /// </summary>
    public class DrawingEngine : IDisposable
    {
        private readonly ILogService _logService;
        private readonly LineRenderer _lineRenderer;
        private readonly PriceLabelRenderer _labelRenderer;
        private bool _disposed = false;

        #region 绘制配置

        /// <summary>
        /// 绘制配置类
        /// </summary>
        public class DrawingConfig
        {
            /// <summary>
            /// 是否启用抗锯齿
            /// </summary>
            public bool EnableAntiAliasing { get; set; } = true;

            /// <summary>
            /// 是否显示网格
            /// </summary>
            public bool ShowGrid { get; set; } = false;

            /// <summary>
            /// 网格间距
            /// </summary>
            public float GridSpacing { get; set; } = 50.0f;

            /// <summary>
            /// 网格颜色
            /// </summary>
            public Color GridColor { get; set; } = Color.FromArgb(200, 200, 200);

            /// <summary>
            /// 背景颜色
            /// </summary>
            public Color BackgroundColor { get; set; } = Color.Transparent;

            /// <summary>
            /// 当前价格
            /// </summary>
            public double CurrentPrice { get; set; } = 0;

            /// <summary>
            /// 标签位置
            /// </summary>
            public PriceLabelRenderer.LabelPosition DefaultLabelPosition { get; set; } = PriceLabelRenderer.LabelPosition.MiddleRight;

            /// <summary>
            /// 是否显示价格标签
            /// </summary>
            public bool ShowPriceLabels { get; set; } = true;

            /// <summary>
            /// 是否高亮重点线
            /// </summary>
            public bool HighlightKeyLines { get; set; } = true;

            /// <summary>
            /// 线条透明度
            /// </summary>
            public float LineOpacity { get; set; } = 0.8f;

            /// <summary>
            /// 标签透明度
            /// </summary>
            public float LabelOpacity { get; set; } = 0.9f;
        }

        /// <summary>
        /// 当前绘制配置
        /// </summary>
        public DrawingConfig Config { get; private set; }

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化绘图引擎
        /// </summary>
        /// <param name="logService">日志服务</param>
        public DrawingEngine(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _logService.LogInfo("DrawingEngine 初始化");

            try
            {
                // 初始化渲染器
                _lineRenderer = new LineRenderer(_logService);
                _labelRenderer = new PriceLabelRenderer(_logService);

                // 初始化配置
                Config = new DrawingConfig();

                _logService.LogInfo("DrawingEngine 初始化完成");
            }
            catch (Exception ex)
            {
                _logService.LogError($"DrawingEngine 初始化失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~DrawingEngine()
        {
            Dispose(false);
        }

        #endregion

        #region 主要绘制方法

        /// <summary>
        /// 绘制预测线集合
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="lines">预测线集合</param>
        /// <param name="chartBounds">图表区域</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawPredictionLines(Graphics graphics, IEnumerable<PredictionLine> lines, RectangleF chartBounds)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制预测线");
                    return false;
                }

                if (lines == null)
                {
                    _logService.LogWarning("预测线集合为空，无法绘制");
                    return false;
                }

                var lineList = lines.ToList();
                if (lineList.Count == 0)
                {
                    _logService.LogDebug("预测线集合为空，跳过绘制");
                    return true; // 空集合不算错误
                }

                _logService.LogInfo($"开始绘制 {lineList.Count} 条预测线");

                // 配置绘图对象
                ConfigureGraphics(graphics);

                // 绘制背景
                if (Config.BackgroundColor != Color.Transparent)
                {
                    DrawBackground(graphics, chartBounds);
                }

                // 绘制网格
                if (Config.ShowGrid)
                {
                    DrawGrid(graphics, chartBounds);
                }

                // 绘制预测线
                var successCount = DrawPredictionLinesInternal(graphics, lineList, chartBounds);

                // 绘制价格标签
                if (Config.ShowPriceLabels)
                {
                    DrawPriceLabelsInternal(graphics, lineList, chartBounds);
                }

                _logService.LogInfo($"预测线绘制完成，成功绘制 {successCount}/{lineList.Count} 条线");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制预测线集合失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制单条预测线
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="line">预测线</param>
        /// <param name="chartBounds">图表区域</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawSinglePredictionLine(Graphics graphics, PredictionLine line, RectangleF chartBounds)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制预测线");
                    return false;
                }

                if (line == null)
                {
                    _logService.LogWarning("预测线数据为空，无法绘制");
                    return false;
                }

                // 配置绘图对象
                ConfigureGraphics(graphics);

                // 绘制线条
                var lineSuccess = DrawSinglePredictionLineInternal(graphics, line, chartBounds);

                // 绘制标签
                var labelSuccess = true;
                if (Config.ShowPriceLabels && line.ShowPriceLabel)
                {
                    labelSuccess = DrawSinglePriceLabelInternal(graphics, line, chartBounds);
                }

                var success = lineSuccess && labelSuccess;
                _logService.LogDebug($"单条预测线绘制{(success ? "成功" : "失败")}: {line.Name}");
                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制单条预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 清空绘制区域
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">清除区域</param>
        /// <returns>是否清除成功</returns>
        public bool ClearDrawingArea(Graphics graphics, RectangleF bounds)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法清除绘制区域");
                    return false;
                }

                // 填充透明背景
                using var brush = new SolidBrush(Color.Transparent);
                graphics.FillRectangle(brush, bounds);

                _logService.LogDebug($"清除绘制区域成功: {bounds}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"清除绘制区域失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 重绘所有内容
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="lines">预测线集合</param>
        /// <param name="chartBounds">图表区域</param>
        /// <returns>是否重绘成功</returns>
        public bool RedrawAll(Graphics graphics, IEnumerable<PredictionLine> lines, RectangleF chartBounds)
        {
            try
            {
                // 清空区域
                if (!ClearDrawingArea(graphics, chartBounds))
                {
                    _logService.LogWarning("清空绘制区域失败，但继续重绘");
                }

                // 重新绘制
                return DrawPredictionLines(graphics, lines, chartBounds);
            }
            catch (Exception ex)
            {
                _logService.LogError($"重绘所有内容失败: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region 辅助绘制方法

        /// <summary>
        /// 绘制背景
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">背景区域</param>
        private void DrawBackground(Graphics graphics, RectangleF bounds)
        {
            try
            {
                using var brush = new SolidBrush(Config.BackgroundColor);
                graphics.FillRectangle(brush, bounds);
                _logService.LogDebug($"背景绘制成功: {bounds}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制背景失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制网格
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">网格区域</param>
        private void DrawGrid(Graphics graphics, RectangleF bounds)
        {
            try
            {
                _lineRenderer.DrawGrid(graphics, bounds, Config.GridSpacing, Config.GridColor, 0.5f, 0.3f);
                _logService.LogDebug($"网格绘制成功，间距: {Config.GridSpacing}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制网格失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 内部方法：绘制预测线集合
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="lines">预测线集合</param>
        /// <param name="chartBounds">图表区域</param>
        /// <returns>成功绘制的数量</returns>
        private int DrawPredictionLinesInternal(Graphics graphics, List<PredictionLine> lines, RectangleF chartBounds)
        {
            int successCount = 0;

            foreach (var line in lines.OrderBy(l => l.Index)) // 按索引排序绘制
            {
                if (DrawSinglePredictionLineInternal(graphics, line, chartBounds))
                {
                    successCount++;
                }
            }

            return successCount;
        }

        /// <summary>
        /// 内部方法：绘制单条预测线
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="line">预测线</param>
        /// <param name="chartBounds">图表区域</param>
        /// <returns>是否绘制成功</returns>
        private bool DrawSinglePredictionLineInternal(Graphics graphics, PredictionLine line, RectangleF chartBounds)
        {
            try
            {
                // 计算线条的起止点
                var startX = chartBounds.Left;
                var endX = chartBounds.Right;
                var yPosition = (float)line.YPosition;

                // 如果Y位置无效，跳过绘制
                if (double.IsNaN(line.YPosition) || line.YPosition < chartBounds.Top || line.YPosition > chartBounds.Bottom)
                {
                    _logService.LogDebug($"预测线Y位置超出范围，跳过绘制: {line.Name}, Y={line.YPosition}");
                    return false;
                }

                // 应用透明度
                var originalOpacity = line.Opacity;
                line.Opacity *= Config.LineOpacity;

                // 绘制线条
                var success = _lineRenderer.DrawPredictionLine(graphics, line, startX, endX, yPosition);

                // 恢复原始透明度
                line.Opacity = originalOpacity;

                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制单条预测线内部方法失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 内部方法：绘制价格标签集合
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="lines">预测线集合</param>
        /// <param name="chartBounds">图表区域</param>
        private void DrawPriceLabelsInternal(Graphics graphics, List<PredictionLine> lines, RectangleF chartBounds)
        {
            try
            {
                var successCount = _labelRenderer.DrawPredictionLineLabelsBatch(
                    graphics, lines.ToArray(), chartBounds, Config.DefaultLabelPosition, Config.CurrentPrice);

                _logService.LogDebug($"批量绘制价格标签完成，成功: {successCount}/{lines.Count}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"批量绘制价格标签失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 内部方法：绘制单个价格标签
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="line">预测线</param>
        /// <param name="chartBounds">图表区域</param>
        /// <returns>是否绘制成功</returns>
        private bool DrawSinglePriceLabelInternal(Graphics graphics, PredictionLine line, RectangleF chartBounds)
        {
            try
            {
                // 计算参考点
                var referencePoint = new PointF(chartBounds.X + chartBounds.Width / 2, (float)line.YPosition);

                return _labelRenderer.DrawPredictionLineLabel(graphics, line, Config.DefaultLabelPosition, referencePoint, Config.CurrentPrice);
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制单个价格标签失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 配置绘图对象
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        private void ConfigureGraphics(Graphics graphics)
        {
            try
            {
                // 设置抗锯齿
                if (Config.EnableAntiAliasing)
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                }
                else
                {
                    graphics.SmoothingMode = SmoothingMode.None;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                }

                // 设置高质量插值
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                _logService.LogDebug($"绘图对象配置完成，抗锯齿: {Config.EnableAntiAliasing}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"配置绘图对象失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 配置管理方法

        /// <summary>
        /// 更新绘制配置
        /// </summary>
        /// <param name="config">新的配置</param>
        public void UpdateConfig(DrawingConfig config)
        {
            try
            {
                Config = config ?? throw new ArgumentNullException(nameof(config));
                _logService.LogInfo("绘制配置已更新");
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新绘制配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetConfig()
        {
            try
            {
                Config = new DrawingConfig();
                _logService.LogInfo("绘制配置已重置为默认值");
            }
            catch (Exception ex)
            {
                _logService.LogError($"重置绘制配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 启用/禁用抗锯齿
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetAntiAliasing(bool enabled)
        {
            Config.EnableAntiAliasing = enabled;
            _logService.LogInfo($"抗锯齿已{(enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 设置当前价格
        /// </summary>
        /// <param name="price">当前价格</param>
        public void SetCurrentPrice(double price)
        {
            Config.CurrentPrice = price;
            _logService.LogDebug($"当前价格已更新: {price:F2}");
        }

        /// <summary>
        /// 设置标签位置
        /// </summary>
        /// <param name="position">标签位置</param>
        public void SetLabelPosition(PriceLabelRenderer.LabelPosition position)
        {
            Config.DefaultLabelPosition = position;
            _logService.LogDebug($"默认标签位置已更新: {position}");
        }

        /// <summary>
        /// 设置线条透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        public void SetLineOpacity(float opacity)
        {
            Config.LineOpacity = Math.Max(0, Math.Min(1, opacity));
            _logService.LogDebug($"线条透明度已更新: {Config.LineOpacity:F2}");
        }

        /// <summary>
        /// 设置标签透明度
        /// </summary>
        /// <param name="opacity">透明度 (0-1)</param>
        public void SetLabelOpacity(float opacity)
        {
            Config.LabelOpacity = Math.Max(0, Math.Min(1, opacity));
            _logService.LogDebug($"标签透明度已更新: {Config.LabelOpacity:F2}");
        }

        /// <summary>
        /// 显示/隐藏网格
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetGridVisibility(bool show)
        {
            Config.ShowGrid = show;
            _logService.LogInfo($"网格显示已{(show ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 显示/隐藏价格标签
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetPriceLabelsVisibility(bool show)
        {
            Config.ShowPriceLabels = show;
            _logService.LogInfo($"价格标签显示已{(show ? "启用" : "禁用")}");
        }

        #endregion

        #region 统计和诊断方法

        /// <summary>
        /// 获取引擎状态信息
        /// </summary>
        /// <returns>状态信息字典</returns>
        public Dictionary<string, object> GetEngineStatus()
        {
            try
            {
                return new Dictionary<string, object>
                {
                    ["IsDisposed"] = _disposed,
                    ["EnableAntiAliasing"] = Config.EnableAntiAliasing,
                    ["ShowGrid"] = Config.ShowGrid,
                    ["ShowPriceLabels"] = Config.ShowPriceLabels,
                    ["HighlightKeyLines"] = Config.HighlightKeyLines,
                    ["CurrentPrice"] = Config.CurrentPrice,
                    ["DefaultLabelPosition"] = Config.DefaultLabelPosition.ToString(),
                    ["LineOpacity"] = Config.LineOpacity,
                    ["LabelOpacity"] = Config.LabelOpacity,
                    ["GridSpacing"] = Config.GridSpacing
                };
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取引擎状态失败: {ex.Message}", ex);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 测试绘制功能
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="testBounds">测试区域</param>
        /// <returns>测试结果</returns>
        public bool TestDrawingFunctionality(Graphics graphics, RectangleF testBounds)
        {
            try
            {
                _logService.LogInfo("开始测试绘制功能");

                // 创建测试预测线
                var testLines = new[]
                {
                    PredictionLine.CreateStandard(0, "A线", 100.0, 100.0, 120.0, 0.0),
                    PredictionLine.CreateStandard(1, "B线", 120.0, 100.0, 120.0, 1.0),
                    PredictionLine.CreateStandard(3, "3线", 137.4, 100.0, 120.0, 3.137)
                };

                // 设置Y位置
                for (int i = 0; i < testLines.Length; i++)
                {
                    testLines[i].YPosition = testBounds.Top + (i + 1) * testBounds.Height / 4;
                }

                // 执行绘制测试
                var success = DrawPredictionLines(graphics, testLines, testBounds);

                _logService.LogInfo($"绘制功能测试{(success ? "通过" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制功能测试失败: {ex.Message}", ex);
                return false;
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
                    _logService.LogInfo("DrawingEngine 正在释放托管资源");
                }

                // 释放子组件资源
                _lineRenderer?.Dispose();
                _labelRenderer?.Dispose();

                _disposed = true;
                _logService.LogInfo("DrawingEngine 资源释放完成");
            }
        }

        #endregion
    }
}