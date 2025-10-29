using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 价格标签渲染器
    /// 提供基于GDI+的价格标签绘制功能，支持多种样式和布局
    /// </summary>
    public class PriceLabelRenderer : IDisposable
    {
        private readonly ILogService _logService;
        private bool _disposed = false;
        private Font _defaultFont;
        private SolidBrush _backgroundBrush;
        private SolidBrush _textBrush;
        private Pen _borderPen;

        #region 样式常量

        /// <summary>
        /// 默认字体名称
        /// </summary>
        public const string DEFAULT_FONT_NAME = "Microsoft YaHei";

        /// <summary>
        /// 默认字体大小
        /// </summary>
        public const float DEFAULT_FONT_SIZE = 10.0f;

        /// <summary>
        /// 小字体大小
        /// </summary>
        public const float SMALL_FONT_SIZE = 8.0f;

        /// <summary>
        /// 大字体大小
        /// </summary>
        public const float LARGE_FONT_SIZE = 12.0f;

        /// <summary>
        /// 默认内边距
        /// </summary>
        public const int DEFAULT_PADDING = 4;

        /// <summary>
        /// 默认边框宽度
        /// </summary>
        public const int DEFAULT_BORDER_WIDTH = 1;

        /// <summary>
        /// 默认圆角半径
        /// </summary>
        public const int DEFAULT_CORNER_RADIUS = 3;

        /// <summary>
        /// 背景颜色
        /// </summary>
        public static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(240, 240, 240);

        /// <summary>
        /// 文本颜色
        /// </summary>
        public static readonly Color DEFAULT_TEXT_COLOR = Color.FromArgb(51, 51, 51);

        /// <summary>
        /// 边框颜色
        /// </summary>
        public static readonly Color DEFAULT_BORDER_COLOR = Color.FromArgb(153, 153, 153);

        /// <summary>
        /// 上涨颜色（红色）
        /// </summary>
        public static readonly Color RISE_COLOR = Color.FromArgb(220, 20, 60);

        /// <summary>
        /// 下跌颜色（绿色）
        /// </summary>
        public static readonly Color FALL_COLOR = Color.FromArgb(50, 205, 50);

        /// <summary>
        /// 重点标签背景颜色
        /// </summary>
        public static readonly Color KEY_LABEL_BACKGROUND = Color.FromArgb(255, 248, 220); // Cornsilk

        #endregion

        #region 标签位置枚举

        /// <summary>
        /// 标签位置枚举
        /// </summary>
        public enum LabelPosition
        {
            /// <summary>
            /// 左上角
            /// </summary>
            TopLeft,

            /// <summary>
            /// 左侧中间
            /// </summary>
            MiddleLeft,

            /// <summary>
            /// 左下角
            /// </summary>
            BottomLeft,

            /// <summary>
            /// 右上角
            /// </summary>
            TopRight,

            /// <summary>
            /// 右侧中间
            /// </summary>
            MiddleRight,

            /// <summary>
            /// 右下角
            /// </summary>
            BottomRight,

            /// <summary>
            /// 居中
            /// </summary>
            Center
        }

        #endregion

        #region 构造函数和析构函数

        /// <summary>
        /// 初始化价格标签渲染器
        /// </summary>
        /// <param name="logService">日志服务</param>
        public PriceLabelRenderer(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _logService.LogInfo("PriceLabelRenderer 初始化");

            InitializeDrawingObjects();
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~PriceLabelRenderer()
        {
            Dispose(false);
        }

        #endregion

        #region 绘制方法

        /// <summary>
        /// 绘制预测线价格标签
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="line">预测线数据</param>
        /// <param name="position">标签位置</param>
        /// <param name="referencePoint">参考点（通常是线条上的一个点）</param>
        /// <param name="currentPrice">当前价格（用于计算涨跌）</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawPredictionLineLabel(Graphics graphics, PredictionLine line, LabelPosition position,
            PointF referencePoint, double currentPrice = 0)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制价格标签");
                    return false;
                }

                if (line == null)
                {
                    _logService.LogWarning("预测线数据为空，无法绘制标签");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 生成标签文本
                var labelText = GenerateLabelText(line, currentPrice);
                if (string.IsNullOrEmpty(labelText))
                {
                    _logService.LogDebug("标签文本为空，跳过绘制");
                    return false;
                }

                // 获取标签样式
                var labelStyle = GetLabelStyle(line);

                // 计算标签位置
                var labelBounds = CalculateLabelBounds(graphics, labelText, position, referencePoint, labelStyle);

                // 绘制标签
                DrawLabel(graphics, labelText, labelBounds, labelStyle);

                _logService.LogDebug($"成功绘制预测线标签: {line.Name}, 文本: {labelText}");

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制预测线标签失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制自定义价格标签
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="text">标签文本</param>
        /// <param name="position">标签位置</param>
        /// <param name="referencePoint">参考点</param>
        /// <param name="backgroundColor">背景颜色</param>
        /// <param name="textColor">文本颜色</param>
        /// <param name="borderColor">边框颜色</param>
        /// <param name="fontSize">字体大小</param>
        /// <param name="padding">内边距</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawCustomLabel(Graphics graphics, string text, LabelPosition position, PointF referencePoint,
            Color backgroundColor, Color textColor, Color borderColor, float fontSize = DEFAULT_FONT_SIZE, int padding = DEFAULT_PADDING)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制自定义标签");
                    return false;
                }

                if (string.IsNullOrEmpty(text))
                {
                    _logService.LogWarning("标签文本为空，无法绘制");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建标签样式
                var labelStyle = new LabelStyle
                {
                    BackgroundColor = backgroundColor,
                    TextColor = textColor,
                    BorderColor = borderColor,
                    FontSize = fontSize,
                    Padding = padding,
                    BorderRadius = DEFAULT_CORNER_RADIUS,
                    BorderWidth = DEFAULT_BORDER_WIDTH
                };

                // 计算标签位置
                var labelBounds = CalculateLabelBounds(graphics, text, position, referencePoint, labelStyle);

                // 绘制标签
                DrawLabel(graphics, text, labelBounds, labelStyle);

                _logService.LogDebug($"成功绘制自定义标签: {text}");

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制自定义标签失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 绘制简单的文本标签（无边框）
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="text">标签文本</param>
        /// <param name="position">标签位置</param>
        /// <param name="referencePoint">参考点</param>
        /// <param name="textColor">文本颜色</param>
        /// <param name="fontSize">字体大小</param>
        /// <param name="backgroundColor">背景颜色（可选）</param>
        /// <returns>是否绘制成功</returns>
        public bool DrawSimpleTextLabel(Graphics graphics, string text, LabelPosition position, PointF referencePoint,
            Color textColor, float fontSize = DEFAULT_FONT_SIZE, Color? backgroundColor = null)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法绘制简单标签");
                    return false;
                }

                if (string.IsNullOrEmpty(text))
                {
                    _logService.LogWarning("标签文本为空，无法绘制");
                    return false;
                }

                // 设置抗锯齿
                ConfigureGraphics(graphics);

                // 创建字体
                using var font = new Font(DEFAULT_FONT_NAME, fontSize, FontStyle.Regular);
                using var textBrush = new SolidBrush(textColor);

                // 测量文本大小
                var textSize = graphics.MeasureString(text, font);

                // 计算标签位置
                var labelBounds = CalculateSimpleLabelBounds(textSize, position, referencePoint);

                // 如果有背景颜色，先绘制背景
                if (backgroundColor.HasValue)
                {
                    using var bgBrush = new SolidBrush(backgroundColor.Value);
                    graphics.FillRectangle(bgBrush, labelBounds);
                }

                // 绘制文本
                graphics.DrawString(text, font, textBrush, labelBounds.Location);

                _logService.LogDebug($"成功绘制简单文本标签: {text}");

                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制简单文本标签失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 批量绘制预测线标签
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="lines">预测线集合</param>
        /// <param name="chartBounds">图表区域</param>
        /// <param name="labelPosition">标签位置</param>
        /// <param name="currentPrice">当前价格</param>
        /// <returns>成功绘制的标签数量</returns>
        public int DrawPredictionLineLabelsBatch(Graphics graphics, PredictionLine[] lines, RectangleF chartBounds,
            LabelPosition labelPosition, double currentPrice = 0)
        {
            try
            {
                if (graphics == null)
                {
                    _logService.LogWarning("绘图对象为空，无法批量绘制标签");
                    return 0;
                }

                if (lines == null || lines.Length == 0)
                {
                    _logService.LogWarning("预测线集合为空，无法批量绘制");
                    return 0;
                }

                int successCount = 0;

                foreach (var line in lines)
                {
                    if (line == null || !line.ShowPriceLabel)
                        continue;

                    // 计算参考点
                    var referencePoint = CalculateReferencePoint(chartBounds, line);

                    // 绘制标签
                    if (DrawPredictionLineLabel(graphics, line, labelPosition, referencePoint, currentPrice))
                    {
                        successCount++;
                    }
                }

                _logService.LogInfo($"批量绘制预测线标签完成，成功: {successCount}/{lines.Length}");

                return successCount;
            }
            catch (Exception ex)
            {
                _logService.LogError($"批量绘制预测线标签失败: {ex.Message}", ex);
                return 0;
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
                _defaultFont = new Font(DEFAULT_FONT_NAME, DEFAULT_FONT_SIZE, FontStyle.Regular);
                _backgroundBrush = new SolidBrush(DEFAULT_BACKGROUND_COLOR);
                _textBrush = new SolidBrush(DEFAULT_TEXT_COLOR);
                _borderPen = new Pen(DEFAULT_BORDER_COLOR, DEFAULT_BORDER_WIDTH);

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
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

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
        /// 生成标签文本
        /// </summary>
        /// <param name="line">预测线</param>
        /// <param name="currentPrice">当前价格</param>
        /// <returns>标签文本</returns>
        private string GenerateLabelText(PredictionLine line, double currentPrice)
        {
            try
            {
                var parts = new System.Collections.Generic.List<string>();

                // 添加线名
                parts.Add(line.Name);

                // 添加价格
                parts.Add($"{line.Price:F2}");

                // 添加百分比距离（如果有当前价格）
                if (currentPrice > 0)
                {
                    var percentage = line.GetPercentageDistanceFromCurrentPrice(currentPrice);
                    parts.Add($"({percentage:+##0.##}%)");
                }

                // 添加置信度（如果低于阈值）
                if (line.Confidence < 0.8)
                {
                    parts.Add($"[{line.Confidence:F1}]");
                }

                return string.Join(" ", parts);
            }
            catch (Exception ex)
            {
                _logService.LogError($"生成标签文本失败: {ex.Message}", ex);
                return line.Name;
            }
        }

        /// <summary>
        /// 获取标签样式
        /// </summary>
        /// <param name="line">预测线</param>
        /// <returns>标签样式</returns>
        private LabelStyle GetLabelStyle(PredictionLine line)
        {
            try
            {
                var style = new LabelStyle
                {
                    BackgroundColor = line.IsKeyLine ? KEY_LABEL_BACKGROUND : DEFAULT_BACKGROUND_COLOR,
                    TextColor = DEFAULT_TEXT_COLOR,
                    BorderColor = DEFAULT_BORDER_COLOR,
                    FontSize = line.IsKeyLine ? LARGE_FONT_SIZE : DEFAULT_FONT_SIZE,
                    Padding = DEFAULT_PADDING,
                    BorderRadius = DEFAULT_CORNER_RADIUS,
                    BorderWidth = DEFAULT_BORDER_WIDTH
                };

                // 根据线索引调整颜色
                switch (line.Index)
                {
                    case 0: // A线
                        style.BorderColor = Color.FromArgb(30, 144, 255);
                        break;
                    case 1: // B线
                        style.BorderColor = Color.FromArgb(50, 205, 50);
                        break;
                    default:
                        // 其他线条使用默认颜色
                        break;
                }

                return style;
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取标签样式失败: {ex.Message}", ex);
                return new LabelStyle(); // 返回默认样式
            }
        }

        /// <summary>
        /// 计算标签边界
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="text">标签文本</param>
        /// <param name="position">标签位置</param>
        /// <param name="referencePoint">参考点</param>
        /// <param name="style">标签样式</param>
        /// <returns>标签边界矩形</returns>
        private RectangleF CalculateLabelBounds(Graphics graphics, string text, LabelPosition position,
            PointF referencePoint, LabelStyle style)
        {
            try
            {
                // 创建临时字体来测量文本
                using var font = new Font(DEFAULT_FONT_NAME, style.FontSize, FontStyle.Regular);
                var textSize = graphics.MeasureString(text, font);

                // 计算标签大小（包含内边距）
                var labelWidth = textSize.Width + style.Padding * 2;
                var labelHeight = textSize.Height + style.Padding * 2;

                // 计算标签位置
                var labelBounds = new RectangleF(0, 0, labelWidth, labelHeight);

                // 根据位置类型调整坐标
                switch (position)
                {
                    case LabelPosition.TopLeft:
                        labelBounds.X = referencePoint.X - labelWidth - 5;
                        labelBounds.Y = referencePoint.Y - labelHeight - 5;
                        break;
                    case LabelPosition.MiddleLeft:
                        labelBounds.X = referencePoint.X - labelWidth - 5;
                        labelBounds.Y = referencePoint.Y - labelHeight / 2;
                        break;
                    case LabelPosition.BottomLeft:
                        labelBounds.X = referencePoint.X - labelWidth - 5;
                        labelBounds.Y = referencePoint.Y + 5;
                        break;
                    case LabelPosition.TopRight:
                        labelBounds.X = referencePoint.X + 5;
                        labelBounds.Y = referencePoint.Y - labelHeight - 5;
                        break;
                    case LabelPosition.MiddleRight:
                        labelBounds.X = referencePoint.X + 5;
                        labelBounds.Y = referencePoint.Y - labelHeight / 2;
                        break;
                    case LabelPosition.BottomRight:
                        labelBounds.X = referencePoint.X + 5;
                        labelBounds.Y = referencePoint.Y + 5;
                        break;
                    case LabelPosition.Center:
                        labelBounds.X = referencePoint.X - labelWidth / 2;
                        labelBounds.Y = referencePoint.Y - labelHeight / 2;
                        break;
                }

                return labelBounds;
            }
            catch (Exception ex)
            {
                _logService.LogError($"计算标签边界失败: {ex.Message}", ex);
                return new RectangleF(referencePoint.X, referencePoint.Y, 100, 20); // 返回默认边界
            }
        }

        /// <summary>
        /// 计算简单标签边界
        /// </summary>
        /// <param name="textSize">文本大小</param>
        /// <param name="position">标签位置</param>
        /// <param name="referencePoint">参考点</param>
        /// <returns>标签边界矩形</returns>
        private RectangleF CalculateSimpleLabelBounds(SizeF textSize, LabelPosition position, PointF referencePoint)
        {
            try
            {
                var labelBounds = new RectangleF(0, 0, textSize.Width, textSize.Height);

                // 根据位置类型调整坐标
                switch (position)
                {
                    case LabelPosition.TopLeft:
                        labelBounds.X = referencePoint.X - textSize.Width;
                        labelBounds.Y = referencePoint.Y - textSize.Height;
                        break;
                    case LabelPosition.MiddleLeft:
                        labelBounds.X = referencePoint.X - textSize.Width;
                        labelBounds.Y = referencePoint.Y - textSize.Height / 2;
                        break;
                    case LabelPosition.BottomLeft:
                        labelBounds.X = referencePoint.X - textSize.Width;
                        labelBounds.Y = referencePoint.Y;
                        break;
                    case LabelPosition.TopRight:
                        labelBounds.X = referencePoint.X;
                        labelBounds.Y = referencePoint.Y - textSize.Height;
                        break;
                    case LabelPosition.MiddleRight:
                        labelBounds.X = referencePoint.X;
                        labelBounds.Y = referencePoint.Y - textSize.Height / 2;
                        break;
                    case LabelPosition.BottomRight:
                        labelBounds.X = referencePoint.X;
                        labelBounds.Y = referencePoint.Y;
                        break;
                    case LabelPosition.Center:
                        labelBounds.X = referencePoint.X - textSize.Width / 2;
                        labelBounds.Y = referencePoint.Y - textSize.Height / 2;
                        break;
                }

                return labelBounds;
            }
            catch (Exception ex)
            {
                _logService.LogError($"计算简单标签边界失败: {ex.Message}", ex);
                return new RectangleF(referencePoint.X, referencePoint.Y, textSize.Width, textSize.Height);
            }
        }

        /// <summary>
        /// 绘制标签
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="text">标签文本</param>
        /// <param name="bounds">标签边界</param>
        /// <param name="style">标签样式</param>
        private void DrawLabel(Graphics graphics, string text, RectangleF bounds, LabelStyle style)
        {
            try
            {
                // 绘制背景（圆角矩形）
                DrawRoundedRectangle(graphics, bounds, style.BorderRadius, style.BackgroundColor);

                // 绘制边框
                using var borderPen = new Pen(style.BorderColor, style.BorderWidth);
                DrawRoundedRectangle(graphics, bounds, style.BorderRadius, borderPen);

                // 绘制文本
                using var font = new Font(DEFAULT_FONT_NAME, style.FontSize, FontStyle.Regular);
                using var textBrush = new SolidBrush(style.TextColor);
                var textRect = new RectangleF(
                    bounds.X + style.Padding,
                    bounds.Y + style.Padding,
                    bounds.Width - style.Padding * 2,
                    bounds.Height - style.Padding * 2);

                graphics.DrawString(text, font, textBrush, textRect);
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制标签失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制圆角矩形
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">边界矩形</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="brush">画刷</param>
        private void DrawRoundedRectangle(Graphics graphics, RectangleF bounds, float cornerRadius, Brush brush)
        {
            try
            {
                var path = CreateRoundedRectanglePath(bounds, cornerRadius);
                graphics.FillPath(brush, path);
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制圆角矩形填充失败: {ex.Message}", ex);
                // 降级为普通矩形
                graphics.FillRectangle(brush, bounds);
            }
        }

        /// <summary>
        /// 绘制圆角矩形边框
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">边界矩形</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="pen">画笔</param>
        private void DrawRoundedRectangle(Graphics graphics, RectangleF bounds, float cornerRadius, Pen pen)
        {
            try
            {
                var path = CreateRoundedRectanglePath(bounds, cornerRadius);
                graphics.DrawPath(pen, path);
            }
            catch (Exception ex)
            {
                _logService.LogError($"绘制圆角矩形边框失败: {ex.Message}", ex);
                // 降级为普通矩形
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        /// <param name="bounds">边界矩形</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <returns>图形路径</returns>
        private GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float cornerRadius)
        {
            try
            {
                var path = new GraphicsPath();

                // 确保圆角半径不超过矩形尺寸的一半
                var radius = Math.Min(cornerRadius, Math.Min(bounds.Width / 2, bounds.Height / 2));

                // 添加圆角矩形的各个边
                path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();

                return path;
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建圆角矩形路径失败: {ex.Message}", ex);
                // 返回普通矩形路径
                var path = new GraphicsPath();
                path.AddRectangle(bounds);
                return path;
            }
        }

        /// <summary>
        /// 计算参考点
        /// </summary>
        /// <param name="chartBounds">图表区域</param>
        /// <param name="line">预测线</param>
        /// <returns>参考点</returns>
        private PointF CalculateReferencePoint(RectangleF chartBounds, PredictionLine line)
        {
            try
            {
                // 使用预测线的Y位置，X位置在图表中间
                return new PointF(chartBounds.X + chartBounds.Width / 2, (float)line.YPosition);
            }
            catch (Exception ex)
            {
                _logService.LogError($"计算参考点失败: {ex.Message}", ex);
                return new PointF(chartBounds.X + chartBounds.Width / 2, chartBounds.Y + chartBounds.Height / 2);
            }
        }

        #endregion

        #region 标签样式类

        /// <summary>
        /// 标签样式类
        /// </summary>
        public class LabelStyle
        {
            /// <summary>
            /// 背景颜色
            /// </summary>
            public Color BackgroundColor { get; set; } = DEFAULT_BACKGROUND_COLOR;

            /// <summary>
            /// 文本颜色
            /// </summary>
            public Color TextColor { get; set; } = DEFAULT_TEXT_COLOR;

            /// <summary>
            /// 边框颜色
            /// </summary>
            public Color BorderColor { get; set; } = DEFAULT_BORDER_COLOR;

            /// <summary>
            /// 字体大小
            /// </summary>
            public float FontSize { get; set; } = DEFAULT_FONT_SIZE;

            /// <summary>
            /// 内边距
            /// </summary>
            public int Padding { get; set; } = DEFAULT_PADDING;

            /// <summary>
            /// 圆角半径
            /// </summary>
            public int BorderRadius { get; set; } = DEFAULT_CORNER_RADIUS;

            /// <summary>
            /// 边框宽度
            /// </summary>
            public int BorderWidth { get; set; } = DEFAULT_BORDER_WIDTH;
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
                    _logService.LogInfo("PriceLabelRenderer 正在释放托管资源");
                }

                // 释放绘图资源
                _defaultFont?.Dispose();
                _backgroundBrush?.Dispose();
                _textBrush?.Dispose();
                _borderPen?.Dispose();

                _disposed = true;
                _logService.LogInfo("PriceLabelRenderer 资源释放完成");
            }
        }

        #endregion
    }
}