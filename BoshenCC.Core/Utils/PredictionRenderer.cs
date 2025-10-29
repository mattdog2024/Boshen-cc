using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using BoshenCC.Models;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 预测线渲染器
    /// 负责将波神预测线渲染到图表上，包括线条、标签、重点线标记等
    /// </summary>
    public static class PredictionRenderer
    {
        #region 渲染配置

        /// <summary>
        /// 默认线条颜色配置
        /// </summary>
        public static readonly PredictionLineColors DefaultColors = new PredictionLineColors
        {
            PointALine = Color.FromArgb(100, 100, 100),     // A线 - 灰色
            PointBLine = Color.FromArgb(100, 100, 100),     // B线 - 灰色
            NormalLine = Color.FromArgb(70, 130, 180),      // 普通线 - 钢蓝色
            KeyLine = Color.FromArgb(255, 69, 0),           // 重点线 - 红橙色
            ExtremeLine = Color.FromArgb(128, 0, 128)       // 极线 - 紫色
        };

        /// <summary>
        /// 默认渲染样式
        /// </summary>
        public static readonly PredictionRenderStyle DefaultStyle = new PredictionRenderStyle
        {
            NormalLineWidth = 1,
            KeyLineWidth = 2,
            ExtremeLineWidth = 2,
            LabelFontSize = 10,
            LabelFontFamily = "Microsoft YaHei",
            LabelBackgroundColor = Color.FromArgb(240, 240, 240),
            LabelBorderColor = Color.FromArgb(150, 150, 150),
            DashPattern = new float[] { 5, 3 },
            ShowLabels = true,
            ShowGroupIndicators = true,
            LabelPadding = 4
        };

        #endregion

        #region 主要渲染方法

        /// <summary>
        /// 渲染预测线到图表
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="lines">预测线列表</param>
        /// <param name="chartBounds">图表边界</param>
        /// <param name="priceRange">价格范围</param>
        /// <param name="renderOptions">渲染选项</param>
        public static void RenderPredictionLines(Graphics graphics, List<PredictionLine> lines,
            Rectangle chartBounds, PriceRange priceRange, PredictionRenderOptions renderOptions = null)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            if (lines == null || lines.Count == 0)
                return;

            if (chartBounds.IsEmpty || priceRange == null)
                return;

            // 使用默认选项
            renderOptions = renderOptions ?? new PredictionRenderOptions();

            // 设置高质量渲染
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 计算每条线的Y坐标位置
            var linePositions = CalculateLinePositions(lines, chartBounds, priceRange);

            // 渲染背景层（如果需要）
            if (renderOptions.ShowBackgroundLayer)
            {
                RenderBackgroundLayer(graphics, linePositions, chartBounds, renderOptions);
            }

            // 渲染预测线
            RenderLines(graphics, lines, linePositions, chartBounds, renderOptions);

            // 渲染标签
            if (renderOptions.ShowLabels)
            {
                RenderLabels(graphics, lines, linePositions, chartBounds, renderOptions);
            }

            // 渲染重点线标记
            if (renderOptions.ShowKeyLineMarkers)
            {
                RenderKeyLineMarkers(graphics, lines, linePositions, chartBounds, renderOptions);
            }

            // 渲染组指示器
            if (renderOptions.ShowGroupIndicators)
            {
                RenderGroupIndicators(graphics, lines, linePositions, chartBounds, renderOptions);
            }
        }

        /// <summary>
        /// 渲染单条预测线
        /// </summary>
        /// <param name="graphics">图形对象</param>
        /// <param name="line">预测线</param>
        /// <param name="chartBounds">图表边界</param>
        /// <param name="priceRange">价格范围</param>
        /// <param name="renderOptions">渲染选项</param>
        public static void RenderSingleLine(Graphics graphics, PredictionLine line,
            Rectangle chartBounds, PriceRange priceRange, PredictionRenderOptions renderOptions = null)
        {
            if (graphics == null || line == null)
                return;

            renderOptions = renderOptions ?? new PredictionRenderOptions();
            var lines = new List<PredictionLine> { line };
            var positions = CalculateLinePositions(lines, chartBounds, priceRange);

            RenderLines(graphics, lines, positions, chartBounds, renderOptions);

            if (renderOptions.ShowLabels)
            {
                RenderLabels(graphics, lines, positions, chartBounds, renderOptions);
            }
        }

        #endregion

        #region 位置计算

        /// <summary>
        /// 计算预测线在图表中的位置
        /// </summary>
        /// <param name="lines">预测线列表</param>
        /// <param name="chartBounds">图表边界</param>
        /// <param name="priceRange">价格范围</param>
        /// <returns>线位置字典</returns>
        private static Dictionary<PredictionLine, float> CalculateLinePositions(List<PredictionLine> lines,
            Rectangle chartBounds, PriceRange priceRange)
        {
            var positions = new Dictionary<PredictionLine, float>();

            if (priceRange.MaxPrice <= priceRange.MinPrice)
                return positions;

            var priceRangeSpan = priceRange.MaxPrice - priceRange.MinPrice;
            var chartHeight = chartBounds.Height;

            foreach (var line in lines)
            {
                // 计算价格在图表中的相对位置
                var relativePosition = (line.Price - priceRange.MinPrice) / priceRangeSpan;
                var yPosition = chartBounds.Bottom - (float)(relativePosition * chartHeight);

                // 确保Y坐标在图表范围内
                yPosition = Math.Max(chartBounds.Top, Math.Min(chartBounds.Bottom, yPosition));

                positions[line] = yPosition;
            }

            return positions;
        }

        #endregion

        #region 渲染组件

        /// <summary>
        /// 渲染背景层
        /// </summary>
        private static void RenderBackgroundLayer(Graphics graphics, Dictionary<PredictionLine, float> linePositions,
            Rectangle chartBounds, PredictionRenderOptions renderOptions)
        {
            if (!renderOptions.ShowBackgroundLayer || linePositions.Count < 2)
                return;

            var sortedLines = linePositions.OrderBy(kv => kv.Value).ToList();

            // 在预测线之间创建渐变背景
            for (int i = 0; i < sortedLines.Count - 1; i++)
            {
                var currentLine = sortedLines[i].Key;
                var nextLine = sortedLines[i + 1].Key;
                var currentY = sortedLines[i].Value;
                var nextY = sortedLines[i + 1].Value;

                // 为重点区域添加背景色
                if (currentLine.IsKeyLine || nextLine.IsKeyLine)
                {
                    var brush = new SolidBrush(Color.FromArgb(20, renderOptions.Colors.KeyLine));
                    var rect = new Rectangle(chartBounds.Left, (int)currentY, chartBounds.Width, (int)(nextY - currentY));
                    graphics.FillRectangle(brush, rect);
                    brush.Dispose();
                }
            }
        }

        /// <summary>
        /// 渲染预测线
        /// </summary>
        private static void RenderLines(Graphics graphics, List<PredictionLine> lines,
            Dictionary<PredictionLine, float> linePositions, Rectangle chartBounds, PredictionRenderOptions renderOptions)
        {
            var style = renderOptions.Style;
            var colors = renderOptions.Colors;

            foreach (var line in lines)
            {
                if (!linePositions.TryGetValue(line, out var yPosition))
                    continue;

                // 确定线条颜色和样式
                var lineColor = GetLineColor(line, colors);
                var lineWidth = GetLineWidth(line, style);
                var dashPattern = GetDashPattern(line, style);

                // 创建画笔
                using var pen = new Pen(lineColor, lineWidth)
                {
                    DashPattern = dashPattern,
                    DashStyle = dashPattern != null ? DashStyle.Custom : DashStyle.Solid
                };

                // 绘制线条
                graphics.DrawLine(pen, chartBounds.Left, yPosition, chartBounds.Right, yPosition);
            }
        }

        /// <summary>
        /// 渲染标签
        /// </summary>
        private static void RenderLabels(Graphics graphics, List<PredictionLine> lines,
            Dictionary<PredictionLine, float> linePositions, Rectangle chartBounds, PredictionRenderOptions renderOptions)
        {
            var style = renderOptions.Style;
            var colors = renderOptions.Colors;

            // 创建字体
            using var font = new Font(style.LabelFontFamily, style.LabelFontSize, FontStyle.Regular);
            using var labelBrush = new SolidBrush(Color.FromArgb(255, 0, 0, 0)); // 黑色文字
            using var borderPen = new Pen(style.LabelBorderColor, 1);

            foreach (var line in lines.Where(l => l.ShowPriceLabel))
            {
                if (!linePositions.TryGetValue(line, out var yPosition))
                    continue;

                var labelText = line.GetPriceLabelText();
                if (string.IsNullOrEmpty(labelText))
                    continue;

                // 测量文本大小
                var textSize = graphics.MeasureString(labelText, font);
                var labelBounds = new Rectangle(
                    chartBounds.Right - (int)textSize.Width - style.LabelPadding * 2 - 5,
                    (int)(yPosition - textSize.Height / 2) - 2,
                    (int)textSize.Width + style.LabelPadding * 2,
                    (int)textSize.Height + 4
                );

                // 确保标签在图表范围内
                if (labelBounds.Right > chartBounds.Right)
                    labelBounds.X = chartBounds.Right - labelBounds.Width - 5;
                if (labelBounds.Top < chartBounds.Top)
                    labelBounds.Top = chartBounds.Top;
                if (labelBounds.Bottom > chartBounds.Bottom)
                    labelBounds.Top = chartBounds.Bottom - labelBounds.Height;

                // 绘制标签背景
                using var backgroundBrush = new SolidBrush(style.LabelBackgroundColor);
                graphics.FillRectangle(backgroundBrush, labelBounds);
                graphics.DrawRectangle(borderPen, labelBounds);

                // 绘制文本
                var textPosition = new Point(
                    labelBounds.Left + style.LabelPadding,
                    labelBounds.Top + 2
                );
                graphics.DrawString(labelText, font, labelBrush, textPosition);
            }
        }

        /// <summary>
        /// 渲染重点线标记
        /// </summary>
        private static void RenderKeyLineMarkers(Graphics graphics, List<PredictionLine> lines,
            Dictionary<PredictionLine, float> linePositions, Rectangle chartBounds, PredictionRenderOptions renderOptions)
        {
            var colors = renderOptions.Colors;
            var keyLines = lines.Where(l => l.IsKeyLine).ToList();

            foreach (var keyLine in keyLines)
            {
                if (!linePositions.TryGetValue(keyLine, out var yPosition))
                    continue;

                // 在重点线左侧添加特殊标记
                var markerX = chartBounds.Left + 20;
                var markerSize = 8;

                using var markerBrush = new SolidBrush(colors.KeyLine);
                using var markerPen = new Pen(colors.KeyLine, 2);

                // 绘制三角形标记
                var markerPath = new GraphicsPath();
                markerPath.AddPolygon(new Point[]
                {
                    new Point(markerX - markerSize / 2, (int)(yPosition - markerSize / 2)),
                    new Point(markerX + markerSize / 2, (int)(yPosition - markerSize / 2)),
                    new Point(markerX, (int)(yPosition + markerSize / 2))
                });

                graphics.FillPath(markerBrush, markerPath);
                graphics.DrawPath(markerPen, markerPath);
                markerPath.Dispose();
            }
        }

        /// <summary>
        /// 渲染组指示器
        /// </summary>
        private static void RenderGroupIndicators(Graphics graphics, List<PredictionLine> lines,
            Dictionary<PredictionLine, float> linePositions, Rectangle chartBounds, PredictionRenderOptions renderOptions)
        {
            // 按组ID分组
            var groupedLines = lines.Where(l => !string.IsNullOrEmpty(l.GroupId))
                                   .GroupBy(l => l.GroupId)
                                   .ToList();

            var style = renderOptions.Style;
            using var groupFont = new Font(style.LabelFontFamily, style.LabelFontSize - 1, FontStyle.Italic);
            using var groupBrush = new SolidBrush(Color.FromArgb(150, 100, 100, 100));

            foreach (var group in groupedLines)
            {
                if (group.Count() < 2)
                    continue;

                // 获取组内最高和最低位置
                var positions = group.Select(l => linePositions[l]).Where(p => p > 0).ToList();
                if (positions.Count < 2)
                    continue;

                var minY = positions.Min();
                var maxY = positions.Max();
                var midY = (minY + maxY) / 2;

                // 在图表左侧显示组标识
                var groupText = $"Group {group.Key.Substring(0, Math.Min(4, group.Key.Length))}";
                var textSize = graphics.MeasureString(groupText, groupFont);
                var textPosition = new Point(chartBounds.Left + 5, (int)(midY - textSize.Height / 2));

                graphics.DrawString(groupText, groupFont, groupBrush, textPosition);
            }
        }

        #endregion

        #region 样式辅助方法

        /// <summary>
        /// 获取线条颜色
        /// </summary>
        private static Color GetLineColor(PredictionLine line, PredictionLineColors colors)
        {
            // 如果线条有自定义颜色，优先使用
            if (line.Color != Color.Empty && line.Color != Color.Transparent)
                return Color.FromArgb((int)(line.Opacity * 255), line.Color);

            // 根据线条类型返回颜色
            return line.LineType switch
            {
                PredictionLineType.PointA => colors.PointALine,
                PredictionLineType.PointB => colors.PointBLine,
                PredictionLineType.ExtremeLine => colors.ExtremeLine,
                _ => line.IsKeyLine ? colors.KeyLine : colors.NormalLine
            };
        }

        /// <summary>
        /// 获取线条宽度
        /// </summary>
        private static int GetLineWidth(PredictionLine line, PredictionRenderStyle style)
        {
            // 如果线条有自定义宽度，优先使用
            if (line.Width > 0)
                return line.Width;

            // 根据线条类型返回宽度
            return line.LineType switch
            {
                PredictionLineType.ExtremeLine => style.ExtremeLineWidth,
                _ => line.IsKeyLine ? style.KeyLineWidth : style.NormalLineWidth
            };
        }

        /// <summary>
        /// 获取虚线模式
        /// </summary>
        private static float[] GetDashPattern(PredictionLine line, PredictionRenderStyle style)
        {
            // 如果是A线或B线，使用实线
            if (line.Index == 0 || line.Index == 1)
                return null;

            // 根据线条样式返回虚线模式
            return line.Style switch
            {
                PredictionLineStyle.Dashed => new float[] { 8, 4 },
                PredictionLineStyle.Dotted => new float[] { 2, 2 },
                PredictionLineStyle.DashDot => new float[] { 8, 4, 2, 4 },
                _ => style.DashPattern
            };
        }

        #endregion

        #region 实用工具方法

        /// <summary>
        /// 创建默认渲染选项
        /// </summary>
        /// <returns>默认渲染选项</returns>
        public static PredictionRenderOptions CreateDefaultOptions()
        {
            return new PredictionRenderOptions
            {
                Colors = DefaultColors,
                Style = DefaultStyle,
                ShowLabels = true,
                ShowKeyLineMarkers = true,
                ShowGroupIndicators = true,
                ShowBackgroundLayer = false
            };
        }

        /// <summary>
        /// 创建高对比度渲染选项
        /// </summary>
        /// <returns>高对比度渲染选项</returns>
        public static PredictionRenderOptions CreateHighContrastOptions()
        {
            return new PredictionRenderOptions
            {
                Colors = new PredictionLineColors
                {
                    PointALine = Color.Black,
                    PointBLine = Color.Black,
                    NormalLine = Color.Blue,
                    KeyLine = Color.Red,
                    ExtremeLine = Color.Purple
                },
                Style = new PredictionRenderStyle
                {
                    NormalLineWidth = 2,
                    KeyLineWidth = 3,
                    ExtremeLineWidth = 3,
                    LabelFontSize = 12,
                    LabelFontFamily = "Arial",
                    LabelBackgroundColor = Color.White,
                    LabelBorderColor = Color.Black,
                    DashPattern = new float[] { 10, 5 }
                },
                ShowLabels = true,
                ShowKeyLineMarkers = true,
                ShowGroupIndicators = false,
                ShowBackgroundLayer = true
            };
        }

        /// <summary>
        /// 创建简约渲染选项
        /// </summary>
        /// <returns>简约渲染选项</returns>
        public static PredictionRenderOptions CreateMinimalOptions()
        {
            return new PredictionRenderOptions
            {
                Colors = new PredictionLineColors
                {
                    PointALine = Color.FromArgb(150, 150, 150),
                    PointBLine = Color.FromArgb(150, 150, 150),
                    NormalLine = Color.FromArgb(100, 100, 200),
                    KeyLine = Color.FromArgb(200, 100, 100),
                    ExtremeLine = Color.FromArgb(150, 100, 150)
                },
                Style = new PredictionRenderStyle
                {
                    NormalLineWidth = 1,
                    KeyLineWidth = 1,
                    ExtremeLineWidth = 1,
                    LabelFontSize = 9,
                    LabelFontFamily = "Arial",
                    DashPattern = new float[] { 3, 2 }
                },
                ShowLabels = false,
                ShowKeyLineMarkers = false,
                ShowGroupIndicators = false,
                ShowBackgroundLayer = false
            };
        }

        /// <summary>
        /// 渲染预览图
        /// </summary>
        /// <param name="lines">预测线列表</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="renderOptions">渲染选项</param>
        /// <returns>预览图像</returns>
        public static Bitmap RenderPreview(List<PredictionLine> lines, int width, int height,
            PredictionRenderOptions renderOptions = null)
        {
            if (lines == null || lines.Count == 0)
                return new Bitmap(width, height);

            var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);

            // 设置白色背景
            graphics.Clear(Color.White);

            // 计算价格范围
            var minPrice = lines.Min(l => l.Price);
            var maxPrice = lines.Max(l => l.Price);
            var priceRange = new PriceRange { MinPrice = minPrice, MaxPrice = maxPrice };

            // 添加边距
            var chartBounds = new Rectangle(50, 20, width - 100, height - 40);

            // 渲染预测线
            RenderPredictionLines(graphics, lines, chartBounds, priceRange, renderOptions);

            return bitmap;
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 价格范围
    /// </summary>
    public class PriceRange
    {
        /// <summary>
        /// 最低价格
        /// </summary>
        public double MinPrice { get; set; }

        /// <summary>
        /// 最高价格
        /// </summary>
        public double MaxPrice { get; set; }
    }

    /// <summary>
    /// 预测线颜色配置
    /// </summary>
    public class PredictionLineColors
    {
        /// <summary>
        /// A线颜色
        /// </summary>
        public Color PointALine { get; set; }

        /// <summary>
        /// B线颜色
        /// </summary>
        public Color PointBLine { get; set; }

        /// <summary>
        /// 普通线颜色
        /// </summary>
        public Color NormalLine { get; set; }

        /// <summary>
        /// 重点线颜色
        /// </summary>
        public Color KeyLine { get; set; }

        /// <summary>
        /// 极线颜色
        /// </summary>
        public Color ExtremeLine { get; set; }
    }

    /// <summary>
    /// 预测线渲染样式
    /// </summary>
    public class PredictionRenderStyle
    {
        /// <summary>
        /// 普通线宽度
        /// </summary>
        public int NormalLineWidth { get; set; }

        /// <summary>
        /// 重点线宽度
        /// </summary>
        public int KeyLineWidth { get; set; }

        /// <summary>
        /// 极线宽度
        /// </summary>
        public int ExtremeLineWidth { get; set; }

        /// <summary>
        /// 标签字体大小
        /// </summary>
        public int LabelFontSize { get; set; }

        /// <summary>
        /// 标签字体
        /// </summary>
        public string LabelFontFamily { get; set; }

        /// <summary>
        /// 标签背景颜色
        /// </summary>
        public Color LabelBackgroundColor { get; set; }

        /// <summary>
        /// 标签边框颜色
        /// </summary>
        public Color LabelBorderColor { get; set; }

        /// <summary>
        /// 虚线模式
        /// </summary>
        public float[] DashPattern { get; set; }

        /// <summary>
        /// 是否显示标签
        /// </summary>
        public bool ShowLabels { get; set; }

        /// <summary>
        /// 是否显示组指示器
        /// </summary>
        public bool ShowGroupIndicators { get; set; }

        /// <summary>
        /// 标签内边距
        /// </summary>
        public int LabelPadding { get; set; }
    }

    /// <summary>
    /// 预测线渲染选项
    /// </summary>
    public class PredictionRenderOptions
    {
        /// <summary>
        /// 颜色配置
        /// </summary>
        public PredictionLineColors Colors { get; set; } = PredictionRenderer.DefaultColors;

        /// <summary>
        /// 渲染样式
        /// </summary>
        public PredictionRenderStyle Style { get; set; } = PredictionRenderer.DefaultStyle;

        /// <summary>
        /// 是否显示标签
        /// </summary>
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// 是否显示重点线标记
        /// </summary>
        public bool ShowKeyLineMarkers { get; set; } = true;

        /// <summary>
        /// 是否显示组指示器
        /// </summary>
        public bool ShowGroupIndicators { get; set; } = true;

        /// <summary>
        /// 是否显示背景层
        /// </summary>
        public bool ShowBackgroundLayer { get; set; } = false;
    }

    #endregion
}