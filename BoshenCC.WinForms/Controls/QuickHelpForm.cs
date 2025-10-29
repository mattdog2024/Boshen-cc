using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 快速帮助窗体
    /// 显示简短的提示信息
    /// </summary>
    public partial class QuickHelpForm : Form
    {
        #region 私有字段

        private readonly HelpTopic _helpTopic;
        private HelpTheme _theme;
        private Timer _autoCloseTimer;
        private bool _isAnimating;
        private float _animationProgress;
        private Point _targetPosition;
        private Point _startPosition;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化QuickHelpForm类
        /// </summary>
        /// <param name="topic">帮助主题</param>
        /// <param name="theme">主题</param>
        public QuickHelpForm(HelpTopic topic, HelpTheme theme = HelpTheme.Default)
        {
            _helpTopic = topic ?? throw new ArgumentNullException(nameof(topic));
            _theme = theme;

            InitializeComponent();
            InitializeTheme();
            CalculateSize();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 在指定位置显示
        /// </summary>
        /// <param name="position">显示位置</param>
        public void ShowAt(Point position)
        {
            _targetPosition = position;
            _startPosition = new Point(position.X, position.Y - 50);

            Location = _startPosition;
            Show();

            StartAnimation();
            StartAutoCloseTimer();
        }

        /// <summary>
        /// 更新主题
        /// </summary>
        /// <param name="theme">新主题</param>
        public void UpdateTheme(HelpTheme theme)
        {
            _theme = theme;
            InitializeTheme();
            Invalidate();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

            // 创建自动关闭定时器
            _autoCloseTimer = new Timer { Interval = 5000 };
            _autoCloseTimer.Tick += OnAutoCloseTimerTick;
        }

        /// <summary>
        /// 初始化主题
        /// </summary>
        private void InitializeTheme()
        {
            switch (_theme)
            {
                case HelpTheme.Dark:
                    BackColor = Color.FromArgb(45, 45, 48);
                    ForeColor = Color.White;
                    break;
                case HelpTheme.Light:
                    BackColor = Color.White;
                    ForeColor = Color.Black;
                    break;
                default:
                    BackColor = Color.FromArgb(248, 249, 250);
                    ForeColor = Color.FromArgb(33, 37, 41);
                    break;
            }
        }

        /// <summary>
        /// 计算窗体大小
        /// </summary>
        private void CalculateSize()
        {
            using (var g = CreateGraphics())
            {
                var titleSize = g.MeasureString(_helpTopic.Title, new Font("Microsoft YaHei", 10f, FontStyle.Bold));
                var messageSize = g.MeasureString(_helpTopic.Message, new Font("Microsoft YaHei", 9f), 300, StringFormat.GenericDefault);

                var width = Math.Max((int)Math.Ceiling(titleSize.Width), (int)Math.Ceiling(messageSize.Width)) + 40;
                var height = (int)Math.Ceiling(titleSize.Height) + (int)Math.Ceiling(messageSize.Height) + 60;

                Size = new Size(Math.Min(width, 350), Math.Min(height, 200));
            }
        }

        /// <summary>
        /// 开始动画
        /// </summary>
        private void StartAnimation()
        {
            _isAnimating = true;
            _animationProgress = 0f;
            var animationTimer = new Timer { Interval = 16 };
            animationTimer.Tick += (s, e) =>
            {
                _animationProgress += 0.1f;
                if (_animationProgress >= 1f)
                {
                    _animationProgress = 1f;
                    _isAnimating = false;
                    animationTimer.Stop();
                    animationTimer.Dispose();
                }

                // 插值计算位置
                var currentX = _startPosition.X + (int)((_targetPosition.X - _startPosition.X) * _animationProgress);
                var currentY = _startPosition.Y + (int)((_targetPosition.Y - _startPosition.Y) * _animationProgress);
                Location = new Point(currentX, currentY);

                // 淡入效果
                Opacity = _animationProgress;
            };
            animationTimer.Start();
        }

        /// <summary>
        /// 开始自动关闭定时器
        /// </summary>
        private void StartAutoCloseTimer()
        {
            _autoCloseTimer.Start();
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 自动关闭定时器事件
        /// </summary>
        private void OnAutoCloseTimerTick(object sender, EventArgs e)
        {
            _autoCloseTimer.Stop();
            CloseWithAnimation();
        }

        /// <summary>
        /// 窗体绘制事件
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = ClientRectangle;

            // 绘制背景
            using (var brush = new SolidBrush(BackColor))
            {
                DrawRoundedRectangle(g, rect, 8, brush);
            }

            // 绘制边框
            var borderColor = _theme == HelpTheme.Dark ? Color.FromArgb(107, 107, 107) : Color.FromArgb(222, 226, 230);
            using (var pen = new Pen(borderColor, 1))
            {
                DrawRoundedRectangle(g, rect, 8, pen);
            }

            // 绘制标题
            if (!string.IsNullOrEmpty(_helpTopic.Title))
            {
                var titleColor = _theme == HelpTheme.Dark ? Color.FromArgb(107, 190, 255) : Color.FromArgb(0, 123, 255);
                using (var titleFont = new Font("Microsoft YaHei", 10f, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(titleColor))
                {
                    var titleRect = new Rectangle(20, 15, Width - 40, 25);
                    g.DrawString(_helpTopic.Title, titleFont, titleBrush, titleRect);
                }
            }

            // 绘制消息
            if (!string.IsNullOrEmpty(_helpTopic.Message))
            {
                using (var messageFont = new Font("Microsoft YaHei", 9f))
                using (var messageBrush = new SolidBrush(ForeColor))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near
                })
                {
                    var messageRect = new Rectangle(20, 45, Width - 40, Height - 65);
                    g.DrawString(_helpTopic.Message, messageFont, messageBrush, messageRect, format);
                }
            }

            // 绘制关闭按钮
            DrawCloseButton(g);
        }

        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            var closeButtonRect = new Rectangle(Width - 30, 10, 20, 20);
            if (closeButtonRect.Contains(e.Location))
            {
                CloseWithAnimation();
            }
        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var closeButtonRect = new Rectangle(Width - 30, 10, 20, 20);
            Cursor = closeButtonRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        /// <summary>
        /// 带动画关闭窗体
        /// </summary>
        private void CloseWithAnimation()
        {
            var fadeTimer = new Timer { Interval = 16 };
            fadeTimer.Tick += (s, e) =>
            {
                Opacity -= 0.1f;
                if (Opacity <= 0)
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    Close();
                }
            };
            fadeTimer.Start();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 绘制圆角矩形
        /// </summary>
        /// <param name="g">绘图对象</param>
        /// <param name="rect">矩形区域</param>
        /// <param name="radius">圆角半径</param>
        /// <param name="brush">画刷</param>
        private void DrawRoundedRectangle(Graphics g, Rectangle rect, int radius, Brush brush)
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

                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 绘制圆角矩形边框
        /// </summary>
        /// <param name="g">绘图对象</param>
        /// <param name="rect">矩形区域</param>
        /// <param name="radius">圆角半径</param>
        /// <param name="pen">画笔</param>
        private void DrawRoundedRectangle(Graphics g, Rectangle rect, int radius, Pen pen)
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

                g.DrawPath(pen, path);
            }
        }

        /// <summary>
        /// 绘制关闭按钮
        /// </summary>
        /// <param name="g">绘图对象</param>
        private void DrawCloseButton(Graphics g)
        {
            var closeButtonRect = new Rectangle(Width - 30, 10, 20, 20);

            // 绘制圆形背景
            var isHover = closeButtonRect.Contains(PointToClient(Cursor.Position));
            var backgroundColor = isHover ? Color.FromArgb(220, 53, 69) : Color.FromArgb(108, 117, 125);

            using (var brush = new SolidBrush(backgroundColor))
            {
                g.FillEllipse(brush, closeButtonRect);
            }

            // 绘制X符号
            using (var pen = new Pen(Color.White, 2))
            {
                var centerX = closeButtonRect.X + closeButtonRect.Width / 2;
                var centerY = closeButtonRect.Y + closeButtonRect.Height / 2;
                var offset = 5;

                g.DrawLine(pen, centerX - offset, centerY - offset, centerX + offset, centerY + offset);
                g.DrawLine(pen, centerX + offset, centerY - offset, centerX - offset, centerY + offset);
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoCloseTimer?.Stop();
                _autoCloseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}