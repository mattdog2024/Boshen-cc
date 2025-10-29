using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 线条渲染器
    /// 提供基于GDI+的抗锯齿线条绘制功能，支持多种线条样式和颜色
    /// </summary>
    public class LineRenderer : IDisposable
    {
        private readonly ILogService _logService;
        private bool _disposed = false;
        private Pen _pen;
        private SolidBrush _brush;

        #region 绘制样式常量

        /// <summary>
        /// 默认线条宽度
        /// </summary>
        public const float DEFAULT_LINE_WIDTH = 1.0f;

        /// <summary>
        /// 重点线条宽度
        /// </summary>
        public const float KEY_LINE_WIDTH = 2.0f;

        /// <summary>
        /// 默认透明度
        /// </summary>
        public const float DEFAULT_OPACITY = 0.8f;

        /// <summary>
        /// 预测线默认颜色（红色）
        /// </summary>
        public static readonly Color DEFAULT_PREDICTION_COLOR = Color.FromArgb(255, 220, 20, 60); // Crimson

        /// <summary>
        /// A线颜色（蓝色）
        /// </summary>
        public static readonly Color POINT_A_COLOR = Color.FromArgb(255, 30, 144, 255); // DodgerBlue

        /// <summary>
        /// B线颜色（绿色）
        /// </summary>
        public static readonly Color POINT_B_COLOR = Color.FromArgb(255, 50, 205, 50); // LimeGreen

        /// <summary>
        /// 重点线颜色（加粗红色）
        /// </summary>
        public static readonly Color KEY_LINE_COLOR = Color.FromArgb(255, 178, 34, 34); // FireBrick

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化线条渲染器
        /// </summary>
        /// <param name="logService">日志服务</param>
        public LineRenderer(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _logService.LogInfo("LineRenderer 初始化");

            InitializeDrawingObjects();
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~LineRenderer()
        {
            Dispose(false);
        }

        #endregion

        #region 绘制方法

        /// <summary>
        /// 绘制预测线
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="line">预测线数据</param>
        /// <param name="startX">起点X坐标</param>
        /// <param name="endX">终点X坐标</param>
        /// <param name="yPosition">Y坐标位置</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawPredictionLine(Graphics graphics, PredictionLine line, float startX, float endX, float yPosition)
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

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建线条样式
                var pen = CreatePredictionLinePen(line);
                if (pen == null)
                {
                    _logService.LogError("创建画笔失败");
                    return false;
                }

                // 绘制线条
                var startPoint = new PointF(startX, yPosition);
                var endPoint = new PointF(endX, yPosition);

                graphics.DrawLine(pen, startPoint, endPoint);

                _logService.LogDebug($"成功绘制预测线: {line.Name}, 位置({startX:F1},{yPosition:F1}) -> ({endX:F1},{yPosition:F1})");

                // 释放画笔资源
                pen.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制预测线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制水平线
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="startX">起点X坐标</param>
        /// <param name="endX">终点X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="color">线条颜色</param>
        /// <param name="width">线条宽度</param>
        /// <param name="style">线条样式</param>
        /// <param name="opacity">透明度 (0-1)</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawHorizontalLine(Graphics graphics, float startX, float endX, float y,
            Color color, float width = DEFAULT_LINE_WIDTH, DashStyle style = DashStyle.Solid, float opacity = DEFAULT_OPACITY)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制水平线");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建画笔
                var pen = CreatePen(color, width, style, opacity);
                if (pen == null)
                {
                    _logService.LogError("创建画笔失败");
                    return false;
                }

                // 绘制线条
                graphics.DrawLine(pen, startX, y, endX, y);

                _logService.LogDebug($"成功绘制水平线: 位置({startX:F1},{y:F1}) -> ({endX:F1},{y:F1})");

                // 释放画笔资源
                pen.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制水平线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制垂直线
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="x">X坐标</param>
        /// <param name="startY">起点Y坐标</param>
        /// <param name="endY">终点Y坐标</param>
        /// <param name="color">线条颜色</param>
        /// <param name="width">线条宽度</param>
        /// <param name="style">线条样式</param>
        /// <param name="opacity">透明度 (0-1)</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawVerticalLine(Graphics graphics, float x, float startY, float endY,
            Color color, float width = DEFAULT_LINE_WIDTH, DashStyle style = DashStyle.Solid, float opacity = DEFAULT_OPACITY)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制垂直线");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建画笔
                var pen = CreatePen(color, width, style, opacity);
                if (pen == null)
                {
                    _logService.LogError("创建画笔失败");
                    return false;
                }

                // 绘制线条
                graphics.DrawLine(pen, x, startY, x, endY);

                _logService.LogDebug($"成功绘制垂直线: 位置({x:F1},{startY:F1}) -> ({x:F1},{endY:F1})");

                // 释放画笔资源
                pen.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制垂直线失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制任意线段
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <param name="color">线条颜色</param>
        /// <param name="width">线条宽度</param>
        /// <param name="style">线条样式</param>
        /// <param name="opacity">透明度 (0-1)</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawLine(Graphics graphics, PointF startPoint, PointF endPoint,
            Color color, float width = DEFAULT_LINE_WIDTH, DashStyle style = DashStyle.Solid, float opacity = DEFAULT_OPACITY)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制线段");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建画笔
                var pen = CreatePen(color, width, style, opacity);
                if (pen == null)
                {
                    _logService.LogError("创建画笔失败");
                    return false;
                }

                // 绘制线条
                graphics.DrawLine(pen, startPoint, endPoint);

                _logService.LogDebug($"成功绘制线段: 位置({startPoint.X:F1},{startPoint.Y:F1}) -> ({endPoint.X:F1},{endPoint.Y:F1})");

                // 释放画笔资源
                pen.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制线段失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制网格线
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制区域</param>
        /// <param name="gridSpacing">网格间距</param>
        /// <param name="color">线条颜色</param>
        /// <param name="width">线条宽度</param>
        /// <param name="opacity">透明度 (0-1)</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawGrid(Graphics graphics, RectangleF bounds, float gridSpacing,
            Color color, float width = 0.5f, float opacity = 0.3f)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制网格");
                    return false;
                }

                if (gridSpacing <= 0)
                {
                    _logService.LogWarning("网格间距必须大于0");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建画笔
                var pen = CreatePen(color, width, DashStyle.Dot, opacity);
                if (pen == null)
                {
                    _logService.LogError("创建画笔失败");
                    return false;
                }

                // 绘制垂直网格线
                for (float x = bounds.X; x <= bounds.Right; x += gridSpacing)
                {
                    graphics.DrawLine(pen, x, bounds.Y, x, bounds.Bottom);
                }

                // 绘制水平网格线
                for (float y = bounds.Y; y <= bounds.Bottom; y += gridSpacing)
                {
                    graphics.DrawLine(pen, bounds.X, y, bounds.Right, y);
                }

                _logService.LogDebug($"成功绘制网格: 区域({bounds.X:F1},{bounds.Y:F1},{bounds.Width:F1},{bounds.Height:F1}), 间距({gridSpacing:F1})");

                // 释放画笔资源
                pen.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制网格失败: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化绘图对象
        /// </summary>
        private void InitializeDrawingObjects()
        {
            try
            {
                _pen = new Pen(DEFAULT_PREDICTION_COLOR, DEFAULT_LINE_WIDTH);
                _brush = new SolidBrush(DEFAULT_PREDICTION_COLOR);

                _logService.LogDebug("绘图对象初始化成功");
            }
            catch (Exception ex)
            {
                _logService.LogError($"初始化绘图对象失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 配置绘图对象的质量设置
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        private void ConfigureGraphics(Graphics graphics)
        {
            try
            {
                // 启用抗锯齿
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 设置高质量插值
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                _logService.LogDebug("绘图对象质量设置配置成功");
            }
            catch (Exception ex)
            {
                _logService.LogError($"配置绘图对象质量设置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据预测线创建画笔
        /// </summary>
        /// <param name="line">预测线</param>
        /// <returns>配置好的画笔</returns>
        private Pen CreatePredictionLinePen(PredictionLine line)
        {
            try
            {
                // 确定颜色
                var color = GetPredictionLineColor(line);

                // 确定线宽
                var width = line.IsKeyLine ? KEY_LINE_WIDTH : (float)line.Width;

                // 确定透明度
                var opacity = (float)line.Opacity;

                // 确定线条样式
                var dashStyle = ConvertToGDIPlusDashStyle(line.Style);

                return CreatePen(color, width, dashStyle, opacity);
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建预测线画笔失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取预测线颜色
        /// </summary>
        /// <param name="line">预测线</param>
        /// <returns>颜色</returns>
        private Color GetPredictionLineColor(PredictionLine line)
        {
            try
            {
                // 根据线索引返回预设颜色
                return line.Index switch
                {
                    0 => POINT_A_COLOR,    // A线 - 蓝色
                    1 => POINT_B_COLOR,    // B线 - 绿色
                    _ => line.IsKeyLine ? KEY_LINE_COLOR : line.Color  // 重点线用加粗红色，其他用自定义颜色
                };
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取预测线颜色失败: {ex.Message}", ex);
                return DEFAULT_PREDICTION_COLOR;
            }
        }

        /// <summary>
        /// 创建画笔
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="width">宽度</param>
        /// <param name="style">线条样式</param>
        /// <param name="opacity">透明度 (0-1)</param>
        /// <returns>配置好的画笔</returns>
        private Pen CreatePen(Color color, float width, DashStyle style, float opacity)
        {
            try
            {
                // 应用透明度
                var alpha = (byte)(255 * Math.Max(0, Math.Min(1, opacity)));
                var penColor = Color.FromArgb(alpha, color.R, color.G, color.B);

                // 创建画笔
                var pen = new Pen(penColor, width)
                {
                    DashStyle = style,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };

                return pen;
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建画笔失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 将PredictionLineStyle转换为GDI+的DashStyle
        /// </summary>
        /// <param name="style">预测线样式</param>
        /// <returns>GDI+线条样式</returns>
        private DashStyle ConvertToGDIPlusDashStyle(PredictionLineStyle style)
        {
            return style switch
            {
                PredictionLineStyle.Solid => DashStyle.Solid,
                PredictionLineStyle.Dashed => DashStyle.Dash,
                PredictionLineStyle.Dotted => DashStyle.Dot,
                PredictionLineStyle.DashDot => DashStyle.DashDot,
                _ => DashStyle.Solid
            };
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
                    _logService.LogInfo("LineRenderer 正在释放托管资源");
                }

                // 释放绘图资源
                _pen?.Dispose();
                _brush?.Dispose();

                _disposed = true;
                _logService.LogInfo("LineRenderer 资源释放完成");
            }
        }

        #endregion
    }
}