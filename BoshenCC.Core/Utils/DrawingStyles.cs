using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using BoshenCC.Models;
using BoshenCC.Services.Interfaces;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 绘制样式管理器
    /// 提供预定义的绘制样式和主题，支持样式配置和自定义
    /// </summary>
    public static class DrawingStyles
    {
        #region 颜色主题

        /// <summary>
        /// 默认主题
        /// </summary>
        public static class DefaultTheme
        {
            /// <summary>
            /// A线颜色（蓝色）
            /// </summary>
            public static readonly Color PointALineColor = Color.FromArgb(255, 30, 144, 255); // DodgerBlue

            /// <summary>
            /// B线颜色（绿色）
            /// </summary>
            public static readonly Color PointBLineColor = Color.FromArgb(255, 50, 205, 50); // LimeGreen

            /// <summary>
            /// 普通预测线颜色（红色）
            /// </summary>
            public static readonly Color NormalLineColor = Color.FromArgb(255, 220, 20, 60); // Crimson

            /// <summary>
            /// 重点线颜色（深红色）
            /// </summary>
            public static readonly Color KeyLineColor = Color.FromArgb(255, 178, 34, 34); // FireBrick

            /// <summary>
            /// 网格颜色（浅灰色）
            /// </summary>
            public static readonly Color GridColor = Color.FromArgb(200, 200, 200);

            /// <summary>
            /// 标签背景颜色
            /// </summary>
            public static readonly Color LabelBackgroundColor = Color.FromArgb(240, 240, 240);

            /// <summary>
            /// 重点标签背景颜色
            /// </summary>
            public static readonly Color KeyLabelBackgroundColor = Color.FromArgb(255, 248, 220); // Cornsilk

            /// <summary>
            /// 文本颜色
            /// </summary>
            public static readonly Color TextColor = Color.FromArgb(51, 51, 51);

            /// <summary>
            /// 边框颜色
            /// </summary>
            public static readonly Color BorderColor = Color.FromArgb(153, 153, 153);
        }

        /// <summary>
        /// 深色主题
        /// </summary>
        public static class DarkTheme
        {
            /// <summary>
            /// A线颜色（亮蓝色）
            /// </summary>
            public static readonly Color PointALineColor = Color.FromArgb(255, 100, 149, 237); // CornflowerBlue

            /// <summary>
            /// B线颜色（亮绿色）
            /// </summary>
            public static readonly Color PointBLineColor = Color.FromArgb(255, 144, 238, 144); // LightGreen

            /// <summary>
            /// 普通预测线颜色（亮红色）
            /// </summary>
            public static readonly Color NormalLineColor = Color.FromArgb(255, 255, 99, 71); // Tomato

            /// <summary>
            /// 重点线颜色（橙色）
            /// </summary>
            public static readonly Color KeyLineColor = Color.FromArgb(255, 255, 140, 0); // DarkOrange

            /// <summary>
            /// 网格颜色（深灰色）
            /// </summary>
            public static readonly Color GridColor = Color.FromArgb(100, 100, 100);

            /// <summary>
            /// 标签背景颜色
            /// </summary>
            public static readonly Color LabelBackgroundColor = Color.FromArgb(45, 45, 45);

            /// <summary>
            /// 重点标签背景颜色
            /// </summary>
            public static readonly Color KeyLabelBackgroundColor = Color.FromArgb(70, 50, 30);

            /// <summary>
            /// 文本颜色
            /// </summary>
            public static readonly Color TextColor = Color.FromArgb(220, 220, 220);

            /// <summary>
            /// 边框颜色
            /// </summary>
            public static readonly Color BorderColor = Color.FromArgb(150, 150, 150);
        }

        /// <summary>
        /// 高对比度主题
        /// </summary>
        public static class HighContrastTheme
        {
            /// <summary>
            /// A线颜色（纯蓝色）
            /// </summary>
            public static readonly Color PointALineColor = Color.FromArgb(255, 0, 0, 255);

            /// <summary>
            /// B线颜色（纯绿色）
            /// </summary>
            public static readonly Color PointBLineColor = Color.FromArgb(255, 0, 128, 0);

            /// <summary>
            /// 普通预测线颜色（纯红色）
            /// </summary>
            public static readonly Color NormalLineColor = Color.FromArgb(255, 255, 0, 0);

            /// <summary>
            /// 重点线颜色（纯黄色）
            /// </summary>
            public static readonly Color KeyLineColor = Color.FromArgb(255, 255, 255, 0);

            /// <summary>
            /// 网格颜色（灰色）
            /// </summary>
            public static readonly Color GridColor = Color.FromArgb(128, 128, 128);

            /// <summary>
            /// 标签背景颜色
            /// </summary>
            public static readonly Color LabelBackgroundColor = Color.FromArgb(255, 255, 255);

            /// <summary>
            /// 重点标签背景颜色
            /// </summary>
            public static readonly Color KeyLabelBackgroundColor = Color.FromArgb(255, 255, 255);

            /// <summary>
            /// 文本颜色
            /// </summary>
            public static readonly Color TextColor = Color.FromArgb(0, 0, 0);

            /// <summary>
            /// 边框颜色
            /// </summary>
            public static readonly Color BorderColor = Color.FromArgb(0, 0, 0);
        }

        #endregion

        #region 线条样式

        /// <summary>
        /// 获取预测线颜色
        /// </summary>
        /// <param name="line">预测线</param>
        /// <param name="theme">主题名称</param>
        /// <returns>线条颜色</returns>
        public static Color GetPredictionLineColor(PredictionLine line, string theme = "Default")
        {
            try
            {
                var themeColors = GetThemeColors(theme);

                return line.Index switch
                {
                    0 => themeColors.PointALineColor,    // A线
                    1 => themeColors.PointBLineColor,    // B线
                    _ => line.IsKeyLine ? themeColors.KeyLineColor : themeColors.NormalLineColor
                };
            }
            catch (Exception ex)
            {
                // 如果获取颜色失败，返回默认颜色
                return DefaultTheme.NormalLineColor;
            }
        }

        /// <summary>
        /// 获取线条宽度
        /// </summary>
        /// <param name="line">预测线</param>
        /// <returns>线条宽度</returns>
        public static float GetLineWidth(PredictionLine line)
        {
            return line.IsKeyLine ? LineRenderer.KEY_LINE_WIDTH : (float)line.Width;
        }

        /// <summary>
        /// 获取线条样式
        /// </summary>
        /// <param name="line">预测线</param>
        /// <returns>GDI+线条样式</returns>
        public static DashStyle GetLineStyle(PredictionLine line)
        {
            return line.Style switch
            {
                PredictionLineStyle.Solid => DashStyle.Solid,
                PredictionLineStyle.Dashed => DashStyle.Dash,
                PredictionLineStyle.Dotted => DashStyle.Dot,
                PredictionLineStyle.DashDot => DashStyle.DashDot,
                _ => DashStyle.Solid
            };
        }

        #endregion

        #region 标签样式

        /// <summary>
        /// 标签样式类
        /// </summary>
        public class LabelStyle
        {
            /// <summary>
            /// 背景颜色
            /// </summary>
            public Color BackgroundColor { get; set; }

            /// <summary>
            /// 文本颜色
            /// </summary>
            public Color TextColor { get; set; }

            /// <summary>
            /// 边框颜色
            /// </summary>
            public Color BorderColor { get; set; }

            /// <summary>
            /// 字体大小
            /// </summary>
            public float FontSize { get; set; }

            /// <summary>
            /// 内边距
            /// </summary>
            public int Padding { get; set; }

            /// <summary>
            /// 圆角半径
            /// </summary>
            public int BorderRadius { get; set; }

            /// <summary>
            /// 边框宽度
            /// </summary>
            public int BorderWidth { get; set; }

            /// <summary>
            /// 创建标签样式
            /// </summary>
            public LabelStyle(Color backgroundColor, Color textColor, Color borderColor,
                float fontSize = PriceLabelRenderer.DEFAULT_FONT_SIZE, int padding = PriceLabelRenderer.DEFAULT_PADDING,
                int borderRadius = PriceLabelRenderer.DEFAULT_CORNER_RADIUS, int borderWidth = PriceLabelRenderer.DEFAULT_BORDER_WIDTH)
            {
                BackgroundColor = backgroundColor;
                TextColor = textColor;
                BorderColor = borderColor;
                FontSize = fontSize;
                Padding = padding;
                BorderRadius = borderRadius;
                BorderWidth = borderWidth;
            }
        }

        /// <summary>
        /// 获取预测线标签样式
        /// </summary>
        /// <param name="line">预测线</param>
        /// <param name="theme">主题名称</param>
        /// <returns>标签样式</returns>
        public static LabelStyle GetPredictionLabelStyle(PredictionLine line, string theme = "Default")
        {
            try
            {
                var themeColors = GetThemeColors(theme);

                var backgroundColor = line.IsKeyLine ? themeColors.KeyLabelBackgroundColor : themeColors.LabelBackgroundColor;
                var fontSize = line.IsKeyLine ? PriceLabelRenderer.LARGE_FONT_SIZE : PriceLabelRenderer.DEFAULT_FONT_SIZE;

                return new LabelStyle(
                    backgroundColor,
                    themeColors.TextColor,
                    themeColors.BorderColor,
                    fontSize
                );
            }
            catch (Exception ex)
            {
                // 返回默认样式
                return new LabelStyle(
                    DefaultTheme.LabelBackgroundColor,
                    DefaultTheme.TextColor,
                    DefaultTheme.BorderColor
                );
            }
        }

        #endregion

        #region 主题管理

        /// <summary>
        /// 主题颜色类
        /// </summary>
        public class ThemeColors
        {
            public Color PointALineColor { get; set; }
            public Color PointBLineColor { get; set; }
            public Color NormalLineColor { get; set; }
            public Color KeyLineColor { get; set; }
            public Color GridColor { get; set; }
            public Color LabelBackgroundColor { get; set; }
            public Color KeyLabelBackgroundColor { get; set; }
            public Color TextColor { get; set; }
            public Color BorderColor { get; set; }
        }

        /// <summary>
        /// 获取主题颜色
        /// </summary>
        /// <param name="themeName">主题名称</param>
        /// <returns>主题颜色</returns>
        public static ThemeColors GetThemeColors(string themeName = "Default")
        {
            return themeName?.ToLowerInvariant() switch
            {
                "dark" or "darktheme" => GetDarkThemeColors(),
                "highcontrast" or "highcontrasttheme" => GetHighContrastThemeColors(),
                "default" or "defaulttheme" or _ => GetDefaultThemeColors()
            };
        }

        /// <summary>
        /// 获取默认主题颜色
        /// </summary>
        /// <returns>默认主题颜色</returns>
        public static ThemeColors GetDefaultThemeColors()
        {
            return new ThemeColors
            {
                PointALineColor = DefaultTheme.PointALineColor,
                PointBLineColor = DefaultTheme.PointBLineColor,
                NormalLineColor = DefaultTheme.NormalLineColor,
                KeyLineColor = DefaultTheme.KeyLineColor,
                GridColor = DefaultTheme.GridColor,
                LabelBackgroundColor = DefaultTheme.LabelBackgroundColor,
                KeyLabelBackgroundColor = DefaultTheme.KeyLabelBackgroundColor,
                TextColor = DefaultTheme.TextColor,
                BorderColor = DefaultTheme.BorderColor
            };
        }

        /// <summary>
        /// 获取深色主题颜色
        /// </summary>
        /// <returns>深色主题颜色</returns>
        public static ThemeColors GetDarkThemeColors()
        {
            return new ThemeColors
            {
                PointALineColor = DarkTheme.PointALineColor,
                PointBLineColor = DarkTheme.PointBLineColor,
                NormalLineColor = DarkTheme.NormalLineColor,
                KeyLineColor = DarkTheme.KeyLineColor,
                GridColor = DarkTheme.GridColor,
                LabelBackgroundColor = DarkTheme.LabelBackgroundColor,
                KeyLabelBackgroundColor = DarkTheme.KeyLabelBackgroundColor,
                TextColor = DarkTheme.TextColor,
                BorderColor = DarkTheme.BorderColor
            };
        }

        /// <summary>
        /// 获取高对比度主题颜色
        /// </summary>
        /// <returns>高对比度主题颜色</returns>
        public static ThemeColors GetHighContrastThemeColors()
        {
            return new ThemeColors
            {
                PointALineColor = HighContrastTheme.PointALineColor,
                PointBLineColor = HighContrastTheme.PointBLineColor,
                NormalLineColor = HighContrastTheme.NormalLineColor,
                KeyLineColor = HighContrastTheme.KeyLineColor,
                GridColor = HighContrastTheme.GridColor,
                LabelBackgroundColor = HighContrastTheme.LabelBackgroundColor,
                KeyLabelBackgroundColor = HighContrastTheme.KeyLabelBackgroundColor,
                TextColor = HighContrastTheme.TextColor,
                BorderColor = HighContrastTheme.BorderColor
            };
        }

        /// <summary>
        /// 获取所有可用主题名称
        /// </summary>
        /// <returns>主题名称列表</returns>
        public static List<string> GetAvailableThemes()
        {
            return new List<string> { "Default", "Dark", "HighContrast" };
        }

        /// <summary>
        /// 创建自定义主题
        /// </summary>
        /// <param name="pointALineColor">A线颜色</param>
        /// <param name="pointBLineColor">B线颜色</param>
        /// <param name="normalLineColor">普通线颜色</param>
        /// <param name="keyLineColor">重点线颜色</param>
        /// <param name="gridColor">网格颜色</param>
        /// <param name="labelBackgroundColor">标签背景颜色</param>
        /// <param name="keyLabelBackgroundColor">重点标签背景颜色</param>
        /// <param name="textColor">文本颜色</param>
        /// <param name="borderColor">边框颜色</param>
        /// <returns>自定义主题颜色</returns>
        public static ThemeColors CreateCustomTheme(
            Color pointALineColor, Color pointBLineColor, Color normalLineColor, Color keyLineColor,
            Color gridColor, Color labelBackgroundColor, Color keyLabelBackgroundColor,
            Color textColor, Color borderColor)
        {
            return new ThemeColors
            {
                PointALineColor = pointALineColor,
                PointBLineColor = pointBLineColor,
                NormalLineColor = normalLineColor,
                KeyLineColor = keyLineColor,
                GridColor = gridColor,
                LabelBackgroundColor = labelBackgroundColor,
                KeyLabelBackgroundColor = keyLabelBackgroundColor,
                TextColor = textColor,
                BorderColor = borderColor
            };
        }

        #endregion

        #region 样式预设

        /// <summary>
        /// 绘制预设类
        /// </summary>
        public static class DrawingPresets
        {
            /// <summary>
            /// 高性能绘制配置
            /// </summary>
            public static DrawingEngine.DrawingConfig HighPerformance => new DrawingEngine.DrawingConfig
            {
                EnableAntiAliasing = false,
                ShowGrid = false,
                ShowPriceLabels = false,
                LineOpacity = 0.6f,
                LabelOpacity = 0.8f,
                CurrentPrice = 0,
                DefaultLabelPosition = PriceLabelRenderer.LabelPosition.MiddleRight,
                HighlightKeyLines = false,
                GridSpacing = 0,
                BackgroundColor = Color.Transparent
            };

            /// <summary>
            /// 高质量绘制配置
            /// </summary>
            public static DrawingEngine.DrawingConfig HighQuality => new DrawingEngine.DrawingConfig
            {
                EnableAntiAliasing = true,
                ShowGrid = true,
                ShowPriceLabels = true,
                LineOpacity = 0.9f,
                LabelOpacity = 1.0f,
                CurrentPrice = 0,
                DefaultLabelPosition = PriceLabelRenderer.LabelPosition.MiddleRight,
                HighlightKeyLines = true,
                GridSpacing = 50.0f,
                BackgroundColor = Color.Transparent
            };

            /// <summary>
            /// 简洁绘制配置
            /// </summary>
            public static DrawingEngine.DrawingConfig Minimal => new DrawingEngine.DrawingConfig
            {
                EnableAntiAliasing = true,
                ShowGrid = false,
                ShowPriceLabels = true,
                LineOpacity = 0.7f,
                LabelOpacity = 0.8f,
                CurrentPrice = 0,
                DefaultLabelPosition = PriceLabelRenderer.LabelPosition.TopRight,
                HighlightKeyLines = true,
                GridSpacing = 0,
                BackgroundColor = Color.Transparent
            };

            /// <summary>
            /// 专业分析配置
            /// </summary>
            public static DrawingEngine.DrawingConfig Professional => new DrawingEngine.DrawingConfig
            {
                EnableAntiAliasing = true,
                ShowGrid = true,
                ShowPriceLabels = true,
                LineOpacity = 0.8f,
                LabelOpacity = 0.9f,
                CurrentPrice = 0,
                DefaultLabelPosition = PriceLabelRenderer.LabelPosition.MiddleRight,
                HighlightKeyLines = true,
                GridSpacing = 25.0f,
                BackgroundColor = Color.FromArgb(248, 248, 248)
            };

            /// <summary>
            /// 演示配置
            /// </summary>
            public static DrawingEngine.DrawingConfig Presentation => new DrawingEngine.DrawingConfig
            {
                EnableAntiAliasing = true,
                ShowGrid = true,
                ShowPriceLabels = true,
                LineOpacity = 1.0f,
                LabelOpacity = 1.0f,
                CurrentPrice = 0,
                DefaultLabelPosition = PriceLabelRenderer.LabelPosition.TopLeft,
                HighlightKeyLines = true,
                GridSpacing = 40.0f,
                BackgroundColor = Color.White
            };
        }

        /// <summary>
        /// 获取绘制预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>绘制配置</returns>
        public static DrawingEngine.DrawingConfig GetDrawingPreset(string presetName = "HighQuality")
        {
            return presetName?.ToLowerInvariant() switch
            {
                "highperformance" or "performance" => DrawingPresets.HighPerformance,
                "highquality" or "quality" or "hq" => DrawingPresets.HighQuality,
                "minimal" or "simple" => DrawingPresets.Minimal,
                "professional" or "pro" => DrawingPresets.Professional,
                "presentation" or "demo" => DrawingPresets.Presentation,
                "default" or _ => DrawingPresets.HighQuality
            };
        }

        /// <summary>
        /// 获取所有可用预设名称
        /// </summary>
        /// <returns>预设名称列表</returns>
        public static List<string> GetAvailablePresets()
        {
            return new List<string> { "HighPerformance", "HighQuality", "Minimal", "Professional", "Presentation" };
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 调整颜色亮度
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="factor">亮度因子（>1变亮，<1变暗）</param>
        /// <returns>调整后的颜色</returns>
        public static Color AdjustColorBrightness(Color color, float factor)
        {
            try
            {
                var r = Math.Min(255, (int)(color.R * factor));
                var g = Math.Min(255, (int)(color.G * factor));
                var b = Math.Min(255, (int)(color.B * factor));
                return Color.FromArgb(color.A, r, g, b);
            }
            catch
            {
                return color;
            }
        }

        /// <summary>
        /// 创建透明颜色
        /// </summary>
        /// <param name="color">基础颜色</param>
        /// <param name="opacity">透明度 (0-1)</param>
        /// <returns>透明颜色</returns>
        public static Color CreateTransparentColor(Color color, float opacity)
        {
            try
            {
                var alpha = (byte)(255 * Math.Max(0, Math.Min(1, opacity)));
                return Color.FromArgb(alpha, color.R, color.G, color.B);
            }
            catch
            {
                return color;
            }
        }

        /// <summary>
        /// 混合两种颜色
        /// </summary>
        /// <param name="color1">颜色1</param>
        /// <param name="color2">颜色2</param>
        /// <param name="ratio">混合比例 (0-1, 0表示纯color1, 1表示纯color2)</param>
        /// <returns>混合后的颜色</returns>
        public static Color BlendColors(Color color1, Color color2, float ratio)
        {
            try
            {
                ratio = Math.Max(0, Math.Min(1, ratio));
                var r = (byte)(color1.R * (1 - ratio) + color2.R * ratio);
                var g = (byte)(color1.G * (1 - ratio) + color2.G * ratio);
                var b = (byte)(color1.B * (1 - ratio) + color2.B * ratio);
                var a = (byte)(color1.A * (1 - ratio) + color2.A * ratio);
                return Color.FromArgb(a, r, g, b);
            }
            catch
            {
                return color1;
            }
        }

        /// <summary>
        /// 获取对比色（用于文本）
        /// </summary>
        /// <param name="backgroundColor">背景颜色</param>
        /// <returns>对比色（黑色或白色）</returns>
        public static Color GetContrastColor(Color backgroundColor)
        {
            try
            {
                // 计算亮度
                var brightness = (backgroundColor.R * 299 + backgroundColor.G * 587 + backgroundColor.B * 114) / 1000;
                return brightness > 128 ? Color.Black : Color.White;
            }
            catch
            {
                return Color.Black;
            }
        }

        /// <summary>
        /// 验证颜色配置的有效性
        /// </summary>
        /// <param name="themeColors">主题颜色</param>
        /// <returns>验证结果</returns>
        public static bool ValidateThemeColors(ThemeColors themeColors)
        {
            try
            {
                if (themeColors == null) return false;

                // 检查是否所有颜色都已设置
                var properties = typeof(ThemeColors).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(Color))
                    {
                        var color = (Color)prop.GetValue(themeColors);
                        if (color == null || color.IsEmpty) return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}