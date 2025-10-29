using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 智能工具提示管理器
    /// 提供丰富的工具提示功能，包括智能延迟、上下文相关提示、动画效果等
    /// </summary>
    public class TooltipManager : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        #region 私有字段

        private readonly Dictionary<Control, TooltipInfo> _registeredControls;
        private readonly Dictionary<string, TooltipTemplate> _templates;
        private readonly Timer _displayTimer;
        private readonly Timer _hideTimer;
        private readonly Timer _animationTimer;
        private readonly List<ActiveTooltip> _activeTooltips;

        private Form _tooltipForm;
        private Control _currentControl;
        private Point _currentPosition;
        private bool _isShowing;
        private bool _disposed;
        private TooltipTheme _currentTheme;
        private int _currentDpi;

        #endregion

        #region 事件定义

        /// <summary>
        /// 工具提示显示事件
        /// </summary>
        public event EventHandler<TooltipShownEventArgs> TooltipShown;

        /// <summary>
        /// 工具提示隐藏事件
        /// </summary>
        public event EventHandler<TooltipHiddenEventArgs> TooltipHidden;

        /// <summary>
        /// 工具提示点击事件
        /// </summary>
        public event EventHandler<TooltipClickedEventArgs> TooltipClicked;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化TooltipManager类
        /// </summary>
        public TooltipManager()
        {
            _registeredControls = new Dictionary<Control, TooltipInfo>();
            _templates = new Dictionary<string, TooltipTemplate>();
            _activeTooltips = new List<ActiveTooltip>();

            InitializeTimers();
            InitializeTemplates();
            InitializeTooltipForm();

            _currentTheme = TooltipTheme.Default;
            _currentDpi = DPIHelper.GetCurrentDpi();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取或设置当前主题
        /// </summary>
        public TooltipTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    UpdateTooltipTheme();
                }
            }
        }

        /// <summary>
        /// 获取或设置默认显示延迟（毫秒）
        /// </summary>
        public int DefaultDelay { get; set; } = 1000;

        /// <summary>
        /// 获取或设置默认隐藏延迟（毫秒）
        /// </summary>
        public int DefaultHideDelay { get; set; } = 5000;

        /// <summary>
        /// 获取或设置是否启用动画效果
        /// </summary>
        public bool EnableAnimation { get; set; } = true;

        /// <summary>
        /// 获取或设置是否启用智能定位
        /// </summary>
        public bool EnableSmartPositioning { get; set; } = true;

        /// <summary>
        /// 获取当前显示的工具提示数量
        /// </summary>
        public int ActiveTooltipCount => _activeTooltips.Count;

        #endregion

        #region 公共方法

        /// <summary>
        /// 注册控件的工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="text">提示文本</param>
        /// <param name="title">提示标题</param>
        /// <param name="template">提示模板名称</param>
        public void RegisterTooltip(Control control, string text, string title = null, string template = null)
        {
            if (control == null || string.IsNullOrEmpty(text)) return;

            var info = new TooltipInfo
            {
                Control = control,
                Text = text,
                Title = title,
                TemplateName = template,
                Delay = DefaultDelay,
                HideDelay = DefaultHideDelay,
                Style = GetTemplateStyle(template)
            };

            _registeredControls[control] = info;

            // 订阅控件事件
            control.MouseEnter += OnControlMouseEnter;
            control.MouseLeave += OnControlMouseLeave;
            control.MouseMove += OnControlMouseMove;
            control.Disposed += OnControlDisposed;
        }

        /// <summary>
        /// 注册控件的高级工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="info">工具提示信息</param>
        public void RegisterAdvancedTooltip(Control control, TooltipInfo info)
        {
            if (control == null || info == null) return;

            info.Control = control;
            if (info.Delay <= 0) info.Delay = DefaultDelay;
            if (info.HideDelay <= 0) info.HideDelay = DefaultHideDelay;
            if (info.Style == null) info.Style = GetTemplateStyle(info.TemplateName);

            _registeredControls[control] = info;

            // 订阅控件事件
            control.MouseEnter += OnControlMouseEnter;
            control.MouseLeave += OnControlMouseLeave;
            control.MouseMove += OnControlMouseMove;
            control.Disposed += OnControlDisposed;
        }

        /// <summary>
        /// 取消注册控件的工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        public void UnregisterTooltip(Control control)
        {
            if (control == null || !_registeredControls.ContainsKey(control)) return;

            // 取消订阅控件事件
            control.MouseEnter -= OnControlMouseEnter;
            control.MouseLeave -= OnControlMouseLeave;
            control.MouseMove -= OnControlMouseMove;
            control.Disposed -= OnControlDisposed;

            _registeredControls.Remove(control);

            // 如果当前正在显示该控件的工具提示，则隐藏
            if (_currentControl == control)
            {
                HideTooltip();
            }
        }

        /// <summary>
        /// 显示指定控件的工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="position">显示位置</param>
        public void ShowTooltip(Control control, Point? position = null)
        {
            if (control == null || !_registeredControls.ContainsKey(control)) return;

            var info = _registeredControls[control];
            _currentControl = control;
            _currentPosition = position ?? Cursor.Position;

            // 计算延迟时间
            var delay = GetIntelligentDelay(info);

            // 启动显示定时器
            _displayTimer.Interval = delay;
            _displayTimer.Start();

            // 停止隐藏定时器
            _hideTimer.Stop();
        }

        /// <summary>
        /// 立即显示工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="position">显示位置</param>
        public void ShowTooltipImmediately(Control control, Point? position = null)
        {
            if (control == null || !_registeredControls.ContainsKey(control)) return;

            _currentControl = control;
            _currentPosition = position ?? Cursor.Position;

            _displayTimer.Stop();
            ShowTooltipInternal();
        }

        /// <summary>
        /// 隐藏当前工具提示
        /// </summary>
        public void HideTooltip()
        {
            _displayTimer.Stop();
            _hideTimer.Stop();

            if (_isShowing)
            {
                HideTooltipInternal();
            }
        }

        /// <summary>
        /// 隐藏所有工具提示
        /// </summary>
        public void HideAllTooltips()
        {
            HideTooltip();

            foreach (var tooltip in _activeTooltips.ToList())
            {
                RemoveActiveTooltip(tooltip);
            }
        }

        /// <summary>
        /// 添加自定义模板
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <param name="template">模板样式</param>
        public void AddTemplate(string name, TooltipTemplate template)
        {
            if (!string.IsNullOrEmpty(name) && template != null)
            {
                _templates[name] = template;
            }
        }

        /// <summary>
        /// 显示上下文相关的帮助提示
        /// </summary>
        /// <param name="context">帮助上下文</param>
        /// <param name="position">显示位置</param>
        public void ShowContextHelp(string context, Point? position = null)
        {
            var helpText = GetContextHelpText(context);
            if (!string.IsNullOrEmpty(helpText))
            {
                var tempControl = new Control { Visible = false };
                var info = new TooltipInfo
                {
                    Control = tempControl,
                    Text = helpText,
                    Title = "上下文帮助",
                    Style = GetTemplateStyle("Help"),
                    Delay = 0,
                    HideDelay = 8000
                };

                _registeredControls[tempControl] = info;
                ShowTooltipImmediately(tempControl, position);

                // 自动清理临时控件
                _hideTimer.Tick += (s, e) =>
                {
                    if (_registeredControls.ContainsKey(tempControl))
                    {
                        _registeredControls.Remove(tempControl);
                        tempControl.Dispose();
                    }
                };
            }
        }

        /// <summary>
        /// 显示操作反馈提示
        /// </summary>
        /// <param name="message">反馈消息</param>
        /// <param name="type">反馈类型</param>
        /// <param name="position">显示位置</param>
        public void ShowFeedback(string message, TooltipType type = TooltipType.Info, Point? position = null)
        {
            if (string.IsNullOrEmpty(message)) return;

            var tempControl = new Control { Visible = false };
            var info = new TooltipInfo
            {
                Control = tempControl,
                Text = message,
                Title = null,
                Style = GetFeedbackStyle(type),
                Delay = 0,
                HideDelay = 3000,
                Type = type
            };

            _registeredControls[tempControl] = info;
            ShowTooltipImmediately(tempControl, position);

            // 自动清理临时控件
            var hideTimer = new Timer { Interval = 3500 };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                hideTimer.Dispose();
                if (_registeredControls.ContainsKey(tempControl))
                {
                    _registeredControls.Remove(tempControl);
                    tempControl.Dispose();
                }
            };
            hideTimer.Start();
        }

        /// <summary>
        /// 更新DPI设置
        /// </summary>
        public void UpdateDpi()
        {
            var newDpi = DPIHelper.GetCurrentDpi();
            if (newDpi != _currentDpi)
            {
                _currentDpi = newDpi;
                UpdateTooltipTheme();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitializeTimers()
        {
            _displayTimer = new Timer();
            _displayTimer.Tick += OnDisplayTimerTick;

            _hideTimer = new Timer();
            _hideTimer.Tick += OnHideTimerTick;

            _animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += OnAnimationTimerTick;
        }

        /// <summary>
        /// 初始化模板
        /// </summary>
        private void InitializeTemplates()
        {
            // 默认模板
            _templates["Default"] = new TooltipTemplate
            {
                BackgroundColor = Color.FromArgb(255, 255, 255),
                BorderColor = Color.FromArgb(200, 200, 200),
                TextColor = Color.FromArgb(51, 51, 51),
                TitleColor = Color.FromArgb(0, 102, 204),
                BorderRadius = 6,
                ShadowSize = 8,
                ShadowOpacity = 0.3f,
                Font = new Font("Microsoft YaHei", 9F),
                TitleFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            // 帮助模板
            _templates["Help"] = new TooltipTemplate
            {
                BackgroundColor = Color.FromArgb(248, 249, 250),
                BorderColor = Color.FromArgb(108, 117, 125),
                TextColor = Color.FromArgb(33, 37, 41),
                TitleColor = Color.FromArgb(0, 123, 255),
                BorderRadius = 8,
                ShadowSize = 12,
                ShadowOpacity = 0.4f,
                Font = new Font("Microsoft YaHei", 9F),
                TitleFont = new Font("Microsoft YaHei", 10F, FontStyle.Bold)
            };

            // 成功模板
            _templates["Success"] = new TooltipTemplate
            {
                BackgroundColor = Color.FromArgb(223, 240, 216),
                BorderColor = Color.FromArgb(40, 167, 69),
                TextColor = Color.FromArgb(49, 107, 68),
                TitleColor = Color.FromArgb(40, 167, 69),
                BorderRadius = 6,
                ShadowSize = 8,
                ShadowOpacity = 0.3f,
                Font = new Font("Microsoft YaHei", 9F),
                TitleFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            // 警告模板
            _templates["Warning"] = new TooltipTemplate
            {
                BackgroundColor = Color.FromArgb(255, 248, 220),
                BorderColor = Color.FromArgb(255, 193, 7),
                TextColor = Color.FromArgb(133, 100, 4),
                TitleColor = Color.FromArgb(255, 193, 7),
                BorderRadius = 6,
                ShadowSize = 8,
                ShadowOpacity = 0.3f,
                Font = new Font("Microsoft YaHei", 9F),
                TitleFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            // 错误模板
            _templates["Error"] = new TooltipTemplate
            {
                BackgroundColor = Color.FromArgb(248, 215, 218),
                BorderColor = Color.FromArgb(220, 53, 69),
                TextColor = Color.FromArgb(114, 28, 36),
                TitleColor = Color.FromArgb(220, 53, 69),
                BorderRadius = 6,
                ShadowSize = 8,
                ShadowOpacity = 0.3f,
                Font = new Font("Microsoft YaHei", 9F),
                TitleFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };
        }

        /// <summary>
        /// 初始化工具提示窗体
        /// </summary>
        private void InitializeTooltipForm()
        {
            _tooltipForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = true,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Transparent,
                AutoScaleMode = AutoScaleMode.Dpi
            };

            _tooltipForm.Paint += OnTooltipFormPaint;
            _tooltipForm.MouseClick += OnTooltipFormMouseClick;
        }

        /// <summary>
        /// 获取智能延迟时间
        /// </summary>
        /// <param name="info">工具提示信息</param>
        /// <returns>延迟时间（毫秒）</returns>
        private int GetIntelligentDelay(TooltipInfo info)
        {
            if (info.Delay > 0)
                return info.Delay;

            // 基于控件类型和用户行为调整延迟
            if (_currentControl is Button)
                return 500; // 按钮快速响应
            else if (_currentControl is TextBox || _currentControl is ComboBox)
                return 800; // 文本框稍慢
            else
                return DefaultDelay; // 默认延迟
        }

        /// <summary>
        /// 获取模板样式
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <returns>模板样式</returns>
        private TooltipTemplate GetTemplateStyle(string templateName)
        {
            if (!string.IsNullOrEmpty(templateName) && _templates.ContainsKey(templateName))
            {
                return _templates[templateName];
            }
            return _templates["Default"];
        }

        /// <summary>
        /// 获取反馈样式
        /// </summary>
        /// <param name="type">反馈类型</param>
        /// <returns>模板样式</returns>
        private TooltipTemplate GetFeedbackStyle(TooltipType type)
        {
            switch (type)
            {
                case TooltipType.Success:
                    return _templates["Success"];
                case TooltipType.Warning:
                    return _templates["Warning"];
                case TooltipType.Error:
                    return _templates["Error"];
                default:
                    return _templates["Default"];
            }
        }

        /// <summary>
        /// 获取上下文帮助文本
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns>帮助文本</returns>
        private string GetContextHelpText(string context)
        {
            var helpTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KLineSelector"] = "K线选择器使用说明：\n\n1. 点击K线图表选择测量点\n2. 支持多种测量模式切换\n3. 可进行影线精确测量\n\n快捷键：\n• 1-4：切换测量模式\n• C：清除选择\n• U：撤销操作\n• R：重做操作",
                ["PriceDisplay"] = "价格显示说明：\n\n• 实时显示选中K线价格\n• 包含开高低收完整信息\n• 显示算法计算结果\n• 支持多组预测数据",
                ["SelectionPanel"] = "选择面板功能：\n\n• 管理测量模式切换\n• 提供撤销/重做操作\n• 清除所有选择\n• 查看操作历史记录",
                ["Shortcuts"] = "常用快捷键：\n\n文件操作：\n• Ctrl+O：打开图片\n• Ctrl+S：保存文件\n\n视图操作：\n• F11：全屏切换\n• F5：刷新视图\n\n帮助：\n• F1：显示帮助"
            };

            return helpTexts.TryGetValue(context, out var text) ? text : string.Empty;
        }

        /// <summary>
        /// 计算最佳显示位置
        /// </summary>
        /// <param name="size">工具提示大小</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>最佳位置</returns>
        private Point CalculateOptimalPosition(Size size, Point targetPosition)
        {
            var screenBounds = Screen.FromPoint(targetPosition).WorkingArea;

            // 默认显示在鼠标右下方
            var x = targetPosition.X + 16;
            var y = targetPosition.Y + 16;

            // 检查是否超出屏幕右边界
            if (x + size.Width > screenBounds.Right)
            {
                x = targetPosition.X - size.Width - 16;
                if (x < screenBounds.Left)
                    x = screenBounds.Left + 10;
            }

            // 检查是否超出屏幕下边界
            if (y + size.Height > screenBounds.Bottom)
            {
                y = targetPosition.Y - size.Height - 16;
                if (y < screenBounds.Top)
                    y = screenBounds.Top + 10;
            }

            return new Point(x, y);
        }

        /// <summary>
        /// 测量文本大小
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="font">字体</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <returns>文本大小</returns>
        private Size MeasureText(string text, Font font, int maxWidth = 300)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var textSize = graphics.MeasureString(text, font, maxWidth, StringFormat.GenericDefault);
                return new Size((int)Math.Ceiling(textSize.Width), (int)Math.Ceiling(textSize.Height));
            }
        }

        /// <summary>
        /// 更新工具提示主题
        /// </summary>
        private void UpdateTooltipTheme()
        {
            // 根据当前主题更新样式
            if (_isShowing && _tooltipForm != null)
            {
                _tooltipForm.Invalidate();
            }
        }

        /// <summary>
        /// 绘制圆角矩形
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="rect">矩形区域</param>
        /// <param name="radius">圆角半径</param>
        /// <param name="color">填充颜色</param>
        private void DrawRoundedRectangle(Graphics graphics, Rectangle rect, int radius, Color color)
        {
            using (var path = new GraphicsPath())
            {
                var diameter = radius * 2;
                var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

                path.AddArc(arc, 180, 90);
                arc.X = rect.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = rect.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = rect.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();

                using (var brush = new SolidBrush(color))
                {
                    graphics.FillPath(brush, path);
                }
            }
        }

        /// <summary>
        /// 绘制阴影
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="rect">矩形区域</param>
        /// <param name="radius">圆角半径</param>
        /// <param name="shadowSize">阴影大小</param>
        /// <param name="opacity">透明度</param>
        private void DrawShadow(Graphics graphics, Rectangle rect, int radius, int shadowSize, float opacity)
        {
            var shadowRect = new Rectangle(
                rect.X + shadowSize / 2,
                rect.Y + shadowSize / 2,
                rect.Width,
                rect.Height);

            using (var path = new GraphicsPath())
            {
                var diameter = radius * 2;
                var arc = new Rectangle(shadowRect.Location, new Size(diameter, diameter));

                path.AddArc(arc, 180, 90);
                arc.X = shadowRect.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = shadowRect.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = shadowRect.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();

                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(255 * opacity), 0, 0, 0)))
                {
                    graphics.FillPath(shadowBrush, path);
                }
            }
        }

        #endregion

        #region 显示/隐藏逻辑

        /// <summary>
        /// 内部显示工具提示方法
        /// </summary>
        private void ShowTooltipInternal()
        {
            if (_currentControl == null || !_registeredControls.ContainsKey(_currentControl)) return;

            var info = _registeredControls[_currentControl];
            var style = info.Style ?? _templates["Default"];

            // 计算工具提示大小
            var titleSize = string.IsNullOrEmpty(info.Title) ? Size.Empty : MeasureText(info.Title, style.TitleFont);
            var textSize = MeasureText(info.Text, style.Font, 300);
            var padding = 16;
            var spacing = 8;

            var width = Math.Max(titleSize.Width, textSize.Width) + padding * 2;
            var height = titleSize.Height + (titleSize.Height > 0 ? spacing : 0) + textSize.Height + padding * 2;

            var tooltipSize = new Size(width, height);
            var location = CalculateOptimalPosition(tooltipSize, _currentPosition);

            // 设置窗体位置和大小
            _tooltipForm.SetBounds(location.X, location.Y, tooltipSize.Width, tooltipSize.Height);
            _tooltipForm.Tag = info;

            // 添加到活跃工具提示列表
            var activeTooltip = new ActiveTooltip
            {
                Control = _currentControl,
                Form = _tooltipForm,
                Info = info,
                ShowTime = DateTime.Now,
                StartTime = DateTime.Now,
                Duration = EnableAnimation ? 200 : 0
            };

            _activeTooltips.Add(activeTooltip);

            // 显示窗体
            _tooltipForm.Opacity = 0;
            _tooltipForm.Show();
            _isShowing = true;

            // 启动动画
            if (EnableAnimation)
            {
                _animationTimer.Start();
            }
            else
            {
                _tooltipForm.Opacity = 1;
                _tooltipForm.Invalidate();
            }

            // 启动隐藏定时器
            _hideTimer.Interval = info.HideDelay;
            _hideTimer.Start();

            // 触发显示事件
            OnTooltipShown(info);
        }

        /// <summary>
        /// 内部隐藏工具提示方法
        /// </summary>
        private void HideTooltipInternal()
        {
            if (!_isShowing || _tooltipForm == null) return;

            // 停止动画定时器
            _animationTimer.Stop();

            // 启动淡出动画
            if (EnableAnimation)
            {
                var fadeTimer = new Timer { Interval = 16 };
                fadeTimer.Tick += (s, e) =>
                {
                    _tooltipForm.Opacity -= 0.05;
                    if (_tooltipForm.Opacity <= 0)
                    {
                        fadeTimer.Stop();
                        fadeTimer.Dispose();
                        CompleteHide();
                    }
                };
                fadeTimer.Start();
            }
            else
            {
                CompleteHide();
            }
        }

        /// <summary>
        /// 完成隐藏操作
        /// </summary>
        private void CompleteHide()
        {
            if (_tooltipForm != null && _tooltipForm.Visible)
            {
                var info = _tooltipForm.Tag as TooltipInfo;
                _tooltipForm.Hide();

                // 移除活跃工具提示
                var activeTooltip = _activeTooltips.FirstOrDefault(t => t.Form == _tooltipForm);
                if (activeTooltip != null)
                {
                    _activeTooltips.Remove(activeTooltip);
                    OnTooltipHidden(info);
                }
            }

            _isShowing = false;
            _currentControl = null;
        }

        /// <summary>
        /// 移除活跃工具提示
        /// </summary>
        /// <param name="tooltip">要移除的工具提示</param>
        private void RemoveActiveTooltip(ActiveTooltip tooltip)
        {
            if (tooltip != null)
            {
                _activeTooltips.Remove(tooltip);
                if (tooltip.Form != null && !tooltip.Form.IsDisposed)
                {
                    tooltip.Form.Close();
                    tooltip.Form.Dispose();
                }
            }
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 显示定时器事件处理
        /// </summary>
        private void OnDisplayTimerTick(object sender, EventArgs e)
        {
            _displayTimer.Stop();
            ShowTooltipInternal();
        }

        /// <summary>
        /// 隐藏定时器事件处理
        /// </summary>
        private void OnHideTimerTick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            HideTooltipInternal();
        }

        /// <summary>
        /// 动画定时器事件处理
        /// </summary>
        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            if (_tooltipForm == null || !_tooltipForm.Visible)
            {
                _animationTimer.Stop();
                return;
            }

            var elapsed = (DateTime.Now - _activeTooltips.LastOrDefault()?.StartTime ?? DateTime.Now).TotalMilliseconds;
            var duration = _activeTooltips.LastOrDefault()?.Duration ?? 200;
            var progress = Math.Min(1.0, elapsed / duration);

            // 淡入动画
            if (progress < 1.0)
            {
                _tooltipForm.Opacity = progress;
            }
            else
            {
                _tooltipForm.Opacity = 1;
                _animationTimer.Stop();
            }

            _tooltipForm.Invalidate();
        }

        /// <summary>
        /// 工具提示窗体绘制事件处理
        /// </summary>
        private void OnTooltipFormPaint(object sender, PaintEventArgs e)
        {
            if (_tooltipForm.Tag is not TooltipInfo info) return;

            var style = info.Style ?? _templates["Default"];
            var rect = new Rectangle(0, 0, _tooltipForm.Width, _tooltipForm.Height);

            // 启用抗锯齿
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 绘制阴影
            if (style.ShadowSize > 0)
            {
                DrawShadow(e.Graphics, rect, style.BorderRadius, style.ShadowSize, style.ShadowOpacity);
            }

            // 绘制背景
            DrawRoundedRectangle(e.Graphics, rect, style.BorderRadius, style.BackgroundColor);

            // 绘制边框
            using (var borderPen = new Pen(style.BorderColor, 1))
            {
                e.Graphics.DrawPath(borderPen, CreateRoundedRectPath(rect, style.BorderRadius));
            }

            // 绘制内容
            var contentRect = new Rectangle(16, 16, rect.Width - 32, rect.Height - 32);
            var y = contentRect.Top;

            // 绘制标题
            if (!string.IsNullOrEmpty(info.Title))
            {
                var titleBrush = new SolidBrush(style.TitleColor);
                e.Graphics.DrawString(info.Title, style.TitleFont, titleBrush, contentRect.Left, y);
                y += style.TitleFont.Height + 8;
                titleBrush.Dispose();
            }

            // 绘制文本
            using (var textBrush = new SolidBrush(style.TextColor))
            using (var textFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                e.Graphics.DrawString(info.Text, style.Font, textBrush, new RectangleF(contentRect.Left, y, contentRect.Width, contentRect.Height - (y - contentRect.Top)), textFormat);
            }
        }

        /// <summary>
        /// 工具提示窗体点击事件处理
        /// </summary>
        private void OnTooltipFormMouseClick(object sender, MouseEventArgs e)
        {
            if (_tooltipForm.Tag is TooltipInfo info)
            {
                OnTooltipClicked(info, e.Location);
            }
        }

        /// <summary>
        /// 控件鼠标进入事件处理
        /// </summary>
        private void OnControlMouseEnter(object sender, EventArgs e)
        {
            if (sender is Control control && _registeredControls.ContainsKey(control))
            {
                ShowTooltip(control);
            }
        }

        /// <summary>
        /// 控件鼠标离开事件处理
        /// </summary>
        private void OnControlMouseLeave(object sender, EventArgs e)
        {
            if (sender is Control control && control == _currentControl)
            {
                // 延迟隐藏，给用户移动到工具提示的时间
                _hideTimer.Interval = 300;
                _hideTimer.Start();
            }
        }

        /// <summary>
        /// 控件鼠标移动事件处理
        /// </summary>
        private void OnControlMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Control control && control == _currentControl)
            {
                // 更新鼠标位置
                _currentPosition = control.PointToScreen(e.Location);

                // 如果正在显示，更新位置
                if (_isShowing && EnableSmartPositioning)
                {
                    var info = _registeredControls[control];
                    var style = info.Style ?? _templates["Default"];
                    var titleSize = string.IsNullOrEmpty(info.Title) ? Size.Empty : MeasureText(info.Title, style.TitleFont);
                    var textSize = MeasureText(info.Text, style.Font, 300);
                    var padding = 16;
                    var spacing = 8;

                    var width = Math.Max(titleSize.Width, textSize.Width) + padding * 2;
                    var height = titleSize.Height + (titleSize.Height > 0 ? spacing : 0) + textSize.Height + padding * 2;
                    var tooltipSize = new Size(width, height);
                    var location = CalculateOptimalPosition(tooltipSize, _currentPosition);

                    if (_tooltipForm.Location != location)
                    {
                        _tooltipForm.Location = location;
                    }
                }
            }
        }

        /// <summary>
        /// 控件销毁事件处理
        /// </summary>
        private void OnControlDisposed(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                UnregisterTooltip(control);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <param name="radius">圆角半径</param>
        /// <returns>图形路径</returns>
        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        #endregion

        #region 事件触发方法

        /// <summary>
        /// 触发工具提示显示事件
        /// </summary>
        /// <param name="info">工具提示信息</param>
        protected virtual void OnTooltipShown(TooltipInfo info)
        {
            TooltipShown?.Invoke(this, new TooltipShownEventArgs
            {
                Control = info.Control,
                Text = info.Text,
                Title = info.Title,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发工具提示隐藏事件
        /// </summary>
        /// <param name="info">工具提示信息</param>
        protected virtual void OnTooltipHidden(TooltipInfo info)
        {
            TooltipHidden?.Invoke(this, new TooltipHiddenEventArgs
            {
                Control = info.Control,
                Text = info.Text,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发工具提示点击事件
        /// </summary>
        /// <param name="info">工具提示信息</param>
        /// <param name="location">点击位置</param>
        protected virtual void OnTooltipClicked(TooltipInfo info, Point location)
        {
            TooltipClicked?.Invoke(this, new TooltipClickedEventArgs
            {
                Control = info.Control,
                Text = info.Text,
                ClickLocation = location,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region IDisposable实现

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
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 停止所有定时器
                _displayTimer?.Stop();
                _hideTimer?.Stop();
                _animationTimer?.Stop();

                // 释放定时器
                _displayTimer?.Dispose();
                _hideTimer?.Dispose();
                _animationTimer?.Dispose();

                // 隐藏并释放工具提示窗体
                HideAllTooltips();
                _tooltipForm?.Dispose();

                // 清理资源
                _registeredControls.Clear();
                _templates.Clear();
                _activeTooltips.Clear();

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 工具提示信息
    /// </summary>
    public class TooltipInfo
    {
        public Control Control { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public string TemplateName { get; set; }
        public TooltipTemplate Style { get; set; }
        public int Delay { get; set; }
        public int HideDelay { get; set; }
        public TooltipType Type { get; set; }
        public object Tag { get; set; }
    }

    /// <summary>
    /// 工具提示模板
    /// </summary>
    public class TooltipTemplate
    {
        public Color BackgroundColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Gray;
        public Color TextColor { get; set; } = Color.Black;
        public Color TitleColor { get; set; } = Color.Blue;
        public int BorderRadius { get; set; } = 6;
        public int ShadowSize { get; set; } = 8;
        public float ShadowOpacity { get; set; } = 0.3f;
        public Font Font { get; set; } = new Font("Microsoft YaHei", 9F);
        public Font TitleFont { get; set; } = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
    }

    /// <summary>
    /// 活跃工具提示
    /// </summary>
    public class ActiveTooltip
    {
        public Control Control { get; set; }
        public Form Form { get; set; }
        public TooltipInfo Info { get; set; }
        public DateTime ShowTime { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
    }

    /// <summary>
    /// 工具提示主题
    /// </summary>
    public enum TooltipTheme
    {
        Default,
        Dark,
        Light,
        Blue,
        Green,
        Custom
    }

    /// <summary>
    /// 工具提示类型
    /// </summary>
    public enum TooltipType
    {
        Info,
        Success,
        Warning,
        Error,
        Help
    }

    /// <summary>
    /// 工具提示显示事件参数
    /// </summary>
    public class TooltipShownEventArgs : EventArgs
    {
        public Control Control { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 工具提示隐藏事件参数
    /// </summary>
    public class TooltipHiddenEventArgs : EventArgs
    {
        public Control Control { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 工具提示点击事件参数
    /// </summary>
    public class TooltipClickedEventArgs : EventArgs
    {
        public Control Control { get; set; }
        public string Text { get; set; }
        public Point ClickLocation { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}