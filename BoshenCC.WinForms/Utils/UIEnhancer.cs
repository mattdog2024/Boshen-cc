using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BoshenCC.Core.Utils;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// UI增强工具类 - 提供UI优化和增强功能
    /// Issue #6 Stream A: UI优化和响应式设计
    /// </summary>
    public static class UIEnhancer
    {
        #region 常量定义

        // 动画持续时间（毫秒）
        private const int ANIMATION_DURATION = 300;

        // 默认圆角半径
        private const int DEFAULT_CORNER_RADIUS = 6;

        // 阴影偏移
        private const int SHADOW_OFFSET = 2;

        // 阴影模糊度
        private const int SHADOW_BLUR = 4;

        #endregion

        #region UI增强方法

        /// <summary>
        /// 为控件启用平滑渲染
        /// </summary>
        /// <param name="control">目标控件</param>
        public static void EnableSmoothRendering(Control control)
        {
            try
            {
                if (control == null)
                    return;

                // 设置双缓冲
                var type = control.GetType();
                var property = type.GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (property != null && property.PropertyType == typeof(bool))
                {
                    property.SetValue(control, true);
                }

                // 为支持高质量渲染的控件设置优化选项
                if (control is PictureBox pictureBox)
                {
                    // 高质量图像插值
                    control.Paint += (sender, e) => {
                        if (e.Graphics != null)
                        {
                            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "启用平滑渲染失败");
            }
        }

        /// <summary>
        /// 设置控件的现代样式
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="backColor">背景色</param>
        /// <param name="foreColor">前景色</param>
        /// <param name="borderColor">边框颜色</param>
        /// <param name="borderRadius">圆角半径</param>
        public static void ApplyModernStyle(Control control, Color? backColor = null,
            Color? foreColor = null, Color? borderColor = null, int borderRadius = DEFAULT_CORNER_RADIUS)
        {
            try
            {
                if (control == null)
                    return;

                // 应用颜色
                if (backColor.HasValue)
                    control.BackColor = backColor.Value;
                if (foreColor.HasValue)
                    control.ForeColor = foreColor.Value;

                // 自定义绘制以支持圆角和边框
                control.Paint += (sender, e) => {
                    DrawModernControl(e.Graphics, control.ClientRectangle,
                        borderColor ?? Color.FromArgb(200, 200, 200), borderRadius);
                };

                // 强制重绘
                control.Invalidate();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "应用现代样式失败");
            }
        }

        /// <summary>
        /// 为按钮设置悬停效果
        /// </summary>
        /// <param name="button">目标按钮</param>
        /// <param name="hoverColor">悬停颜色</param>
        /// <param name="normalColor">正常颜色</param>
        public static void SetButtonHoverEffect(Button button, Color? hoverColor = null, Color? normalColor = null)
        {
            try
            {
                if (button == null)
                    return;

                var normal = normalColor ?? button.BackColor;
                var hover = hoverColor ?? ControlPaint.Light(normal, 0.2f);

                button.MouseEnter += (sender, e) => {
                    button.BackColor = hover;
                    button.Cursor = Cursors.Hand;
                };

                button.MouseLeave += (sender, e) => {
                    button.BackColor = normal;
                    button.Cursor = Cursors.Default;
                };

                button.MouseDown += (sender, e) => {
                    button.BackColor = ControlPaint.Dark(normal, 0.1f);
                };

                button.MouseUp += (sender, e) => {
                    button.BackColor = hover;
                };
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "设置按钮悬停效果失败");
            }
        }

        /// <summary>
        /// 为Panel添加渐变背景
        /// </summary>
        /// <param name="panel">目标Panel</param>
        /// <param name="startColor">起始颜色</param>
        /// <param name="endColor">结束颜色</param>
        /// <param name="angle">渐变角度</param>
        public static void SetGradientBackground(Panel panel, Color startColor, Color endColor, float angle = 90f)
        {
            try
            {
                if (panel == null)
                    return;

                panel.Paint += (sender, e) => {
                    var rect = panel.ClientRectangle;
                    using (var brush = new LinearGradientBrush(rect, startColor, endColor, angle))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                };

                panel.Invalidate();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "设置渐变背景失败");
            }
        }

        /// <summary>
        /// 创建阴影效果
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="shadowColor">阴影颜色</param>
        /// <param name="offset">偏移量</param>
        /// <param name="blur">模糊度</param>
        public static void SetShadowEffect(Control control, Color? shadowColor = null,
            int? offset = null, int? blur = null)
        {
            try
            {
                if (control == null)
                    return;

                var color = shadowColor ?? Color.FromArgb(50, 0, 0, 0);
                var shadowOffset = offset ?? SHADOW_OFFSET;
                var shadowBlur = blur ?? SHADOW_BLUR;

                // 使用ControlPaint创建阴影效果
                control.Paint += (sender, e) => {
                    var rect = control.ClientRectangle;
                    var shadowRect = new Rectangle(
                        rect.X + shadowOffset,
                        rect.Y + shadowOffset,
                        rect.Width,
                        rect.Height);

                    // 绘制阴影
                    ControlPaint.DrawBorder(e.Graphics, shadowRect,
                        color, shadowBlur, ButtonBorderStyle.Solid,
                        color, shadowBlur, ButtonBorderStyle.Solid,
                        color, shadowBlur, ButtonBorderStyle.Solid,
                        color, shadowBlur, ButtonBorderStyle.Solid);
                };

                control.Invalidate();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "设置阴影效果失败");
            }
        }

        /// <summary>
        /// 设置控件的透明度
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="opacity">透明度（0-255）</param>
        public static void SetOpacity(Control control, int opacity)
        {
            try
            {
                if (control == null)
                    return;

                opacity = Math.Max(0, Math.Min(255, opacity));

                // 使用SetWindowLong API设置透明度
                // 这里简化处理，实际项目中可以使用P/Invoke
                if (control is Form form)
                {
                    form.Opacity = opacity / 255.0;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "设置透明度失败");
            }
        }

        /// <summary>
        /// 为控件添加加载动画
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="duration">动画持续时间（毫秒）</param>
        public static void AddLoadingAnimation(Control control, int duration = ANIMATION_DURATION)
        {
            try
            {
                if (control == null)
                    return;

                var startTime = DateTime.Now;
                var timer = new Timer { Interval = 16 }; // 约60 FPS
                var originalBackColor = control.BackColor;

                timer.Tick += (sender, e) => {
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    var progress = Math.Min(1.0, elapsed / duration);

                    // 淡入效果
                    var alpha = (int)(255 * progress);
                    var newColor = Color.FromArgb(alpha, originalBackColor);

                    if (progress >= 1.0)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }

                    control.Invalidate();
                };

                timer.Start();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "添加加载动画失败");
            }
        }

        /// <summary>
        /// 优化控件布局
        /// </summary>
        /// <param name="container">容器控件</param>
        public static void OptimizeLayout(Control container)
        {
            try
            {
                if (container == null)
                    return;

                container.SuspendLayout();

                // 为所有子控件启用双缓冲
                EnableSmoothRendering(container);

                // 递归处理子控件
                foreach (Control child in container.Controls)
                {
                    OptimizeLayout(child);
                }

                container.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "优化布局失败");
            }
        }

        /// <summary>
        /// 创建现代化的工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="text">提示文本</param>
        /// <param name="title">标题</param>
        /// <param name="icon">图标类型</param>
        public static void SetModernTooltip(Control control, string text, string title = null, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                if (control == null || string.IsNullOrEmpty(text))
                    return;

                var tooltip = new ToolTip
                {
                    ToolTipIcon = icon,
                    ToolTipTitle = title,
                    UseAnimation = true,
                    UseFading = true,
                    AutoPopDelay = 3000,
                    InitialDelay = 500,
                    ReshowDelay = 100
                };

                tooltip.SetToolTip(control, text);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "设置工具提示失败");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 绘制现代化控件外观
        /// </summary>
        private static void DrawModernControl(Graphics graphics, Rectangle rect, Color borderColor, int radius)
        {
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // 创建圆角矩形路径
                using (var path = CreateRoundedRectanglePath(rect, radius))
                using (var pen = new Pen(borderColor, 1))
                {
                    graphics.DrawPath(pen, path);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "绘制现代化控件失败");
            }
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();

            // 确保半径不会过大
            radius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));

            // 添加圆角矩形的四个角和边
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();

            return path;
        }

        #endregion

        #region 性能优化

        /// <summary>
        /// 批量优化多个控件
        /// </summary>
        /// <param name="controls">控件数组</param>
        public static void OptimizeControls(params Control[] controls)
        {
            try
            {
                if (controls == null || controls.Length == 0)
                    return;

                foreach (var control in controls)
                {
                    if (control != null)
                    {
                        EnableSmoothRendering(control);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "批量优化控件失败");
            }
        }

        /// <summary>
        /// 预加载常用资源
        /// </summary>
        public static void PreloadResources()
        {
            try
            {
                // 预加载常用画刷和画笔
                using (var brush = new SolidBrush(Color.Empty))
                using (var pen = new Pen(Color.Empty))
                {
                    // 强制系统创建GDI+对象
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "UI增强", "预加载资源失败");
            }
        }

        #endregion
    }
}