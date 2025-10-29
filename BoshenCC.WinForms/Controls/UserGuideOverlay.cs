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
    /// 用户引导覆盖层
    /// 提供新用户引导、功能介绍、操作提示等交互式引导功能
    /// </summary>
    public class UserGuideOverlay : Form
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const uint LWA_COLORKEY = 0x00000001;
        private const uint LWA_ALPHA = 0x00000002;

        #endregion

        #region 私有字段

        private readonly List<GuideStep> _guideSteps;
        private readonly Timer _animationTimer;
        private readonly Timer _autoAdvanceTimer;

        private int _currentStepIndex;
        private GuideStep _currentStep;
        private bool _isAnimating;
        private float _animationProgress;
        private GuideTheme _currentTheme;
        private Rectangle _highlightRegion;
        private Rectangle _currentHighlightRect;
        private Point _balloonPosition;
        private Point _currentBalloonPosition;
        private string _guideTitle;
        private Bitmap _backBuffer;
        private bool _disposed;
        private bool _isInteractiveMode;
        private bool _autoAdvance;
        private int _autoAdvanceDelay;

        #endregion

        #region 事件定义

        /// <summary>
        /// 引导开始事件
        /// </summary>
        public event EventHandler<GuideStartedEventArgs> GuideStarted;

        /// <summary>
        /// 引导步骤变化事件
        /// </summary>
        public event EventHandler<GuideStepChangedEventArgs> StepChanged;

        /// <summary>
        /// 引导完成事件
        /// </summary>
        public event EventHandler<GuideCompletedEventArgs> GuideCompleted;

        /// <summary>
        /// 引导跳过事件
        /// </summary>
        public event EventHandler<GuideSkippedEventArgs> GuideSkipped;

        /// <summary>
        /// 用户交互事件
        /// </summary>
        public event EventHandler<GuideInteractionEventArgs> UserInteraction;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化UserGuideOverlay类
        /// </summary>
        public UserGuideOverlay()
        {
            _guideSteps = new List<GuideStep>();
            _currentStepIndex = -1;
            _currentTheme = GuideTheme.Default;
            _isInteractiveMode = true;
            _autoAdvance = false;
            _autoAdvanceDelay = 5000;

            InitializeForm();
            InitializeTimers();
            CreateDefaultGuideSteps();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取或设置当前主题
        /// </summary>
        public GuideTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    UpdateTheme();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 获取或设置是否启用交互模式
        /// </summary>
        public bool IsInteractiveMode
        {
            get => _isInteractiveMode;
            set => _isInteractiveMode = value;
        }

        /// <summary>
        /// 获取或设置是否自动前进
        /// </summary>
        public bool AutoAdvance
        {
            get => _autoAdvance;
            set
            {
                _autoAdvance = value;
                if (_autoAdvance && _currentStep != null)
                {
                    StartAutoAdvanceTimer();
                }
                else
                {
                    StopAutoAdvanceTimer();
                }
            }
        }

        /// <summary>
        /// 获取或设置自动前进延迟（毫秒）
        /// </summary>
        public int AutoAdvanceDelay
        {
            get => _autoAdvanceDelay;
            set => _autoAdvanceDelay = Math.Max(1000, value);
        }

        /// <summary>
        /// 获取当前引导步骤索引
        /// </summary>
        public int CurrentStepIndex => _currentStepIndex;

        /// <summary>
        /// 获取总步骤数
        /// </summary>
        public int TotalSteps => _guideSteps.Count;

        /// <summary>
        /// 获取当前引导步骤
        /// </summary>
        public GuideStep CurrentStep => _currentStep;

        /// <summary>
        /// 获取是否正在引导
        /// </summary>
        public bool IsGuiding => _currentStepIndex >= 0 && _currentStepIndex < _guideSteps.Count;

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加引导步骤
        /// </summary>
        /// <param name="step">引导步骤</param>
        public void AddGuideStep(GuideStep step)
        {
            if (step != null)
            {
                _guideSteps.Add(step);
            }
        }

        /// <summary>
        /// 插入引导步骤
        /// </summary>
        /// <param name="index">插入位置</param>
        /// <param name="step">引导步骤</param>
        public void InsertGuideStep(int index, GuideStep step)
        {
            if (step != null && index >= 0 && index <= _guideSteps.Count)
            {
                _guideSteps.Insert(index, step);
            }
        }

        /// <summary>
        /// 移除引导步骤
        /// </summary>
        /// <param name="step">引导步骤</param>
        public void RemoveGuideStep(GuideStep step)
        {
            _guideSteps.Remove(step);
        }

        /// <summary>
        /// 清空所有引导步骤
        /// </summary>
        public void ClearGuideSteps()
        {
            StopGuide();
            _guideSteps.Clear();
        }

        /// <summary>
        /// 开始引导
        /// </summary>
        /// <param name="title">引导标题</param>
        /// <param name="startIndex">开始步骤索引</param>
        public void StartGuide(string title = "用户引导", int startIndex = 0)
        {
            if (_guideSteps.Count == 0) return;

            _guideTitle = title;
            _currentStepIndex = Math.Max(0, Math.Min(startIndex, _guideSteps.Count - 1));
            _currentStep = _guideSteps[_currentStepIndex];

            Show();
            BringToFront();
            SetForegroundWindow(Handle);

            StartAnimation();
            OnGuideStarted();

            if (_autoAdvance)
            {
                StartAutoAdvanceTimer();
            }
        }

        /// <summary>
        /// 停止引导
        /// </summary>
        public void StopGuide()
        {
            StopAutoAdvanceTimer();
            StopAnimation();
            Hide();

            _currentStepIndex = -1;
            _currentStep = null;
        }

        /// <summary>
        /// 下一步
        /// </summary>
        public void NextStep()
        {
            if (_currentStepIndex < _guideSteps.Count - 1)
            {
                _currentStepIndex++;
                ChangeToStep(_currentStepIndex);
            }
            else
            {
                CompleteGuide();
            }
        }

        /// <summary>
        /// 上一步
        /// </summary>
        public void PreviousStep()
        {
            if (_currentStepIndex > 0)
            {
                _currentStepIndex--;
                ChangeToStep(_currentStepIndex);
            }
        }

        /// <summary>
        /// 跳转到指定步骤
        /// </summary>
        /// <param name="stepIndex">步骤索引</param>
        public void GoToStep(int stepIndex)
        {
            if (stepIndex >= 0 && stepIndex < _guideSteps.Count)
            {
                ChangeToStep(stepIndex);
            }
        }

        /// <summary>
        /// 跳过引导
        /// </summary>
        public void SkipGuide()
        {
            OnGuideSkipped();
            StopGuide();
        }

        /// <summary>
        /// 完成引导
        /// </summary>
        public void CompleteGuide()
        {
            OnGuideCompleted();
            StopGuide();
        }

        /// <summary>
        /// 显示快速引导
        /// </summary>
        /// <param name="step">引导步骤</param>
        /// <param name="duration">显示持续时间（毫秒）</param>
        public void ShowQuickGuide(GuideStep step, int duration = 3000)
        {
            if (step == null) return;

            var tempSteps = new List<GuideStep>(_guideSteps);
            _guideSteps.Clear();
            _guideSteps.Add(step);

            StartGuide("快速提示", 0);

            var timer = new Timer { Interval = duration };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                StopGuide();
                _guideSteps.Clear();
                _guideSteps.AddRange(tempSteps);
            };
            timer.Start();
        }

        /// <summary>
        /// 高亮指定控件
        /// </summary>
        /// <param name="control">要高亮的控件</param>
        /// <param name="message">提示消息</param>
        /// <param name="duration">显示持续时间</param>
        public void HighlightControl(Control control, string message, int duration = 2000)
        {
            if (control == null || !control.Visible) return;

            var step = new GuideStep
            {
                Title = "控件提示",
                Message = message,
                HighlightRegion = control.RectangleToScreen(control.Bounds),
                BalloonPosition = GetOptimalBalloonPosition(control.RectangleToScreen(control.Bounds)),
                ShowButtons = false,
                AllowSkip = true
            };

            ShowQuickGuide(step, duration);
        }

        /// <summary>
        /// 更新高亮区域
        /// </summary>
        /// <param name="rect">高亮区域</param>
        public void UpdateHighlightRegion(Rectangle rect)
        {
            if (_currentStep != null)
            {
                _highlightRegion = rect;
                StartAnimation();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化窗体
        /// </summary>
        private void InitializeForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            StartPosition = FormStartPosition.Manual;

            // 设置分层窗口属性
            SetWindowLong(Handle, GWL_EXSTYLE, GetWindowLong(Handle, GWL_EXSTYLE) | WS_EX_LAYERED);

            // 设置窗体透明
            SetLayeredWindowAttributes(Handle, 0, 200, LWA_ALPHA);

            // 启用双缓冲
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
        }

        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitializeTimers()
        {
            _animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += OnAnimationTimerTick;

            _autoAdvanceTimer = new Timer();
            _autoAdvanceTimer.Tick += OnAutoAdvanceTimerTick;
        }

        /// <summary>
        /// 创建默认引导步骤
        /// </summary>
        private void CreateDefaultGuideSteps()
        {
            // 欢迎步骤
            AddGuideStep(new GuideStep
            {
                Title = "欢迎使用波神K线测量工具",
                Message = "这是一个快速引导，帮助您了解软件的主要功能。\n\n点击"下一步"继续，或点击"跳过"退出引导。",
                ShowButtons = true,
                AllowSkip = true,
                BalloonPosition = new Point(Screen.PrimaryScreen.WorkingArea.Width / 2 - 150, 100)
            });

            // 文件操作步骤
            AddGuideStep(new GuideStep
            {
                Title = "打开K线图片",
                Message = "首先需要打开包含K线的图片文件。\n\n您可以使用以下方法：\n• 点击"文件"菜单，选择"打开"\n• 使用快捷键 Ctrl+O\n• 或者将图片文件拖拽到窗口中",
                HighlightRegion = new Rectangle(10, 10, 200, 30), // 菜单栏的大概位置
                BalloonPosition = new Point(220, 50),
                ShowButtons = true,
                AllowSkip = true
            });

            // K线选择步骤
            AddGuideStep(new GuideStep
            {
                Title = "选择K线进行测量",
                Message = "打开图片后，您可以：\n\n1. 点击K线选择第一个测量点（A点）\n2. 继续点击选择第二个测量点（B点）\n3. 系统会自动计算并显示测量结果\n\n使用数字键1-4可以快速切换测量模式。",
                HighlightRegion = new Rectangle(Screen.PrimaryScreen.WorkingArea.Width / 2 - 200,
                                               Screen.PrimaryScreen.WorkingArea.Height / 2 - 150,
                                               400, 300),
                BalloonPosition = new Point(Screen.PrimaryScreen.WorkingArea.Width / 2 - 150,
                                           Screen.PrimaryScreen.WorkingArea.Height - 200),
                ShowButtons = true,
                AllowSkip = true
            });

            // 结果查看步骤
            AddGuideStep(new GuideStep
            {
                Title = "查看测量结果",
                Message = "测量完成后，结果会显示在右侧面板中：\n\n• 显示价格差和百分比\n• 提供波神算法预测\n• 包含详细的技术分析\n\n您可以导出结果或保存分析报告。",
                HighlightRegion = new Rectangle(Screen.PrimaryScreen.WorkingArea.Width - 250, 100, 230, 400),
                BalloonPosition = new Point(Screen.PrimaryScreen.WorkingArea.Width - 450, 200),
                ShowButtons = true,
                AllowSkip = true
            });

            // 快捷键步骤
            AddGuideStep(new GuideStep
            {
                Title = "使用快捷键提高效率",
                Message = "熟练使用快捷键可以大幅提高操作效率：\n\n• Ctrl+O：打开文件\n• Ctrl+S：保存结果\n• 1-4：切换测量模式\n• C：清除选择\n• U：撤销操作\n• F1：显示帮助",
                HighlightRegion = new Rectangle(Screen.PrimaryScreen.WorkingArea.Width / 2 - 150,
                                               Screen.PrimaryScreen.WorkingArea.Height / 2 - 100,
                                               300, 200),
                BalloonPosition = new Point(100, Screen.PrimaryScreen.WorkingArea.Height / 2),
                ShowButtons = true,
                AllowSkip = true
            });

            // 完成步骤
            AddGuideStep(new GuideStep
            {
                Title = "引导完成",
                Message = "恭喜！您已经了解了软件的基本使用方法。\n\n现在您可以：\n• 开始实际测量操作\n• 查看详细帮助文档（按F1）\n• 探索更多高级功能\n\n祝您使用愉快！",
                ShowButtons = true,
                AllowSkip = false,
                ShowFinishButton = true,
                BalloonPosition = new Point(Screen.PrimaryScreen.WorkingArea.Width / 2 - 150, 100)
            });
        }

        /// <summary>
        /// 更改到指定步骤
        /// </summary>
        /// <param name="stepIndex">步骤索引</param>
        private void ChangeToStep(int stepIndex)
        {
            var previousStep = _currentStep;
            _currentStepIndex = stepIndex;
            _currentStep = _guideSteps[stepIndex];

            if (_currentStep.HighlightRegion != Rectangle.Empty)
            {
                _highlightRegion = _currentStep.HighlightRegion;
            }

            _balloonPosition = _currentStep.BalloonPosition;

            StartAnimation();
            OnStepChanged(previousStep, _currentStep);

            if (_autoAdvance)
            {
                RestartAutoAdvanceTimer();
            }
        }

        /// <summary>
        /// 获取最佳气泡位置
        /// </summary>
        /// <param name="targetRect">目标区域</param>
        /// <returns>最佳位置</returns>
        private Point GetOptimalBalloonPosition(Rectangle targetRect)
        {
            var screenBounds = Screen.PrimaryScreen.WorkingArea;
            var balloonSize = new Size(300, 120); // 估计的气泡大小

            // 默认放在目标区域右下方
            var x = targetRect.Right + 20;
            var y = targetRect.Bottom + 10;

            // 检查是否超出屏幕边界
            if (x + balloonSize.Width > screenBounds.Right)
            {
                x = targetRect.Left - balloonSize.Width - 20;
                if (x < screenBounds.Left)
                    x = screenBounds.Left + 20;
            }

            if (y + balloonSize.Height > screenBounds.Bottom)
            {
                y = targetRect.Top - balloonSize.Height - 10;
                if (y < screenBounds.Top)
                    y = screenBounds.Top + 20;
            }

            return new Point(x, y);
        }

        /// <summary>
        /// 开始动画
        /// </summary>
        private void StartAnimation()
        {
            _isAnimating = true;
            _animationProgress = 0f;
            _currentHighlightRect = _highlightRegion;
            _currentBalloonPosition = _balloonPosition;
            _animationTimer.Start();
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        private void StopAnimation()
        {
            _isAnimating = false;
            _animationProgress = 1f;
            _animationTimer.Stop();
        }

        /// <summary>
        /// 开始自动前进定时器
        /// </summary>
        private void StartAutoAdvanceTimer()
        {
            if (_autoAdvance && _currentStep != null && _currentStep.AutoAdvance)
            {
                _autoAdvanceTimer.Interval = _currentStep.AutoAdvanceDelay ?? _autoAdvanceDelay;
                _autoAdvanceTimer.Start();
            }
        }

        /// <summary>
        /// 停止自动前进定时器
        /// </summary>
        private void StopAutoAdvanceTimer()
        {
            _autoAdvanceTimer.Stop();
        }

        /// <summary>
        /// 重启自动前进定时器
        /// </summary>
        private void RestartAutoAdvanceTimer()
        {
            StopAutoAdvanceTimer();
            StartAutoAdvanceTimer();
        }

        /// <summary>
        /// 更新主题
        /// </summary>
        private void UpdateTheme()
        {
            // 根据主题更新颜色和样式
            switch (_currentTheme)
            {
                case GuideTheme.Dark:
                    // 深色主题设置
                    break;
                case GuideTheme.Light:
                    // 浅色主题设置
                    break;
                case GuideTheme.Blue:
                    // 蓝色主题设置
                    break;
                default:
                    // 默认主题设置
                    break;
            }
        }

        /// <summary>
        /// 获取主题颜色
        /// </summary>
        /// <param name="colorType">颜色类型</param>
        /// <returns>颜色值</returns>
        private Color GetThemeColor(GuideThemeColor colorType)
        {
            switch (_currentTheme)
            {
                case GuideTheme.Dark:
                    return colorType switch
                    {
                        GuideThemeColor.Background => Color.FromArgb(40, 40, 40),
                        GuideThemeColor.Foreground => Color.White,
                        GuideThemeColor.Accent => Color.FromArgb(0, 120, 215),
                        GuideThemeColor.Border => Color.FromArgb(100, 100, 100),
                        _ => Color.Gray
                    };
                case GuideTheme.Light:
                    return colorType switch
                    {
                        GuideThemeColor.Background => Color.White,
                        GuideThemeColor.Foreground => Color.Black,
                        GuideThemeColor.Accent => Color.FromArgb(0, 120, 215),
                        GuideThemeColor.Border => Color.FromArgb(200, 200, 200),
                        _ => Color.Gray
                    };
                default:
                    return colorType switch
                    {
                        GuideThemeColor.Background => Color.FromArgb(50, 50, 50),
                        GuideThemeColor.Foreground => Color.White,
                        GuideThemeColor.Accent => Color.FromArgb(0, 123, 255),
                        GuideThemeColor.Border => Color.FromArgb(150, 150, 150),
                        _ => Color.Gray
                    };
            }
        }

        #endregion

        #region 绘制方法

        /// <summary>
        /// 绘制覆盖层
        /// </summary>
        /// <param name="e">绘制事件参数</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_currentStep == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 绘制半透明背景
            DrawBackground(g);

            // 绘制高亮区域
            if (_highlightRegion != Rectangle.Empty)
            {
                DrawHighlightRegion(g);
            }

            // 绘制提示气泡
            DrawBalloon(g);

            // 绘制进度指示器
            DrawProgressIndicator(g);
        }

        /// <summary>
        /// 绘制背景
        /// </summary>
        /// <param name="g">绘图对象</param>
        private void DrawBackground(Graphics g)
        {
            var backgroundColor = GetThemeColor(GuideThemeColor.Background);
            using (var brush = new SolidBrush(Color.FromArgb(180, backgroundColor)))
            {
                g.FillRectangle(brush, ClientRectangle);
            }
        }

        /// <summary>
        /// 绘制高亮区域
        /// </summary>
        /// <param name="g">绘图对象</param>
        private void DrawHighlightRegion(Graphics g)
        {
            if (_currentHighlightRect.Width <= 0 || _currentHighlightRect.Height <= 0) return;

            // 计算动画插值
            var progress = _isAnimating ? _animationProgress : 1f;
            var highlightRect = new Rectangle(
                _highlightRegion.X - (int)((1 - progress) * 20),
                _highlightRegion.Y - (int)((1 - progress) * 20),
                _highlightRegion.Width + (int)((1 - progress) * 40),
                _highlightRegion.Height + (int)((1 - progress) * 40));

            // 创建高亮路径
            using (var path = new GraphicsPath())
            {
                path.AddRectangle(highlightRect);
                path.AddRectangle(ClientRectangle);

                using (var brush = new SolidBrush(Color.Transparent))
                {
                    g.FillPath(brush, path);
                }
            }

            // 绘制边框
            var accentColor = GetThemeColor(GuideThemeColor.Accent);
            using (var pen = new Pen(accentColor, 3))
            {
                g.DrawRectangle(pen, highlightRect);
            }

            // 绘制脉冲效果
            if (_isAnimating && progress < 1f)
            {
                var pulseAlpha = (int)((1f - progress) * 100);
                var pulseRect = new Rectangle(
                    highlightRect.X - (int)((1f - progress) * 10),
                    highlightRect.Y - (int)((1f - progress) * 10),
                    highlightRect.Width + (int)((1f - progress) * 20),
                    highlightRect.Height + (int)((1f - progress) * 20));

                using (var pulsePen = new Pen(Color.FromArgb(pulseAlpha, accentColor), 2))
                {
                    g.DrawRectangle(pulsePen, pulseRect);
                }
            }
        }

        /// <summary>
        /// 绘制提示气泡
        /// </summary>
        /// <param name="g">绘图对象</param>
        private void DrawBalloon(Graphics g)
        {
            if (string.IsNullOrEmpty(_currentStep?.Title) && string.IsNullOrEmpty(_currentStep?.Message)) return;

            var progress = _isAnimating ? _animationProgress : 1f;
            var balloonRect = new Rectangle(
                _currentBalloonPosition.X,
                _currentBalloonPosition.Y,
                300,
                120);

            // 动画缩放效果
            var scale = 0.8f + 0.2f * progress;
            var scaledRect = new Rectangle(
                balloonRect.X + (int)((1 - scale) * balloonRect.Width / 2),
                balloonRect.Y + (int)((1 - scale) * balloonRect.Height / 2),
                (int)(balloonRect.Width * scale),
                (int)(balloonRect.Height * scale));

            // 绘制气泡背景
            var backgroundColor = GetThemeColor(GuideThemeColor.Background);
            var borderColor = GetThemeColor(GuideThemeColor.Border);
            var foregroundColor = GetThemeColor(GuideThemeColor.Foreground);

            using (var brush = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(brush, scaledRect);
            }

            using (var pen = new Pen(borderColor, 1))
            {
                g.DrawRectangle(pen, scaledRect);
            }

            // 绘制标题
            if (!string.IsNullOrEmpty(_currentStep.Title))
            {
                using (var titleFont = new Font("Microsoft YaHei", 12f, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(foregroundColor))
                {
                    var titleRect = new Rectangle(scaledRect.X + 15, scaledRect.Y + 15, scaledRect.Width - 30, 25);
                    g.DrawString(_currentStep.Title, titleFont, titleBrush, titleRect);
                }
            }

            // 绘制消息
            if (!string.IsNullOrEmpty(_currentStep.Message))
            {
                using (var messageFont = new Font("Microsoft YaHei", 9f))
                using (var messageBrush = new SolidBrush(foregroundColor))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near
                })
                {
                    var messageRect = new Rectangle(
                        scaledRect.X + 15,
                        scaledRect.Y + 45,
                        scaledRect.Width - 30,
                        scaledRect.Height - 60);

                    g.DrawString(_currentStep.Message, messageFont, messageBrush, messageRect, format);
                }
            }

            // 绘制按钮
            if (_currentStep.ShowButtons)
            {
                DrawButtons(g, scaledRect);
            }
        }

        /// <summary>
        /// 绘制按钮
        /// </summary>
        /// <param name="g">绘图对象</param>
        /// <param name="balloonRect">气泡矩形</param>
        private void DrawButtons(Graphics g, Rectangle balloonRect)
        {
            var accentColor = GetThemeColor(GuideThemeColor.Accent);
            var foregroundColor = GetThemeColor(GuideThemeColor.Foreground);
            var buttonY = balloonRect.Bottom - 40;

            // 上一步按钮
            if (_currentStepIndex > 0)
            {
                var prevButtonRect = new Rectangle(balloonRect.X + 10, buttonY, 60, 30);
                DrawButton(g, prevButtonRect, "上一步", foregroundColor);

                // 存储按钮区域用于点击检测
                _prevButtonRect = prevButtonRect;
            }
            else
            {
                _prevButtonRect = Rectangle.Empty;
            }

            // 下一步/完成按钮
            var nextButtonText = _currentStep.ShowFinishButton ? "完成" : "下一步";
            var nextButtonRect = new Rectangle(balloonRect.Right - 70, buttonY, 60, 30);
            DrawButton(g, nextButtonRect, nextButtonText, accentColor);
            _nextButtonRect = nextButtonRect;

            // 跳过按钮
            if (_currentStep.AllowSkip)
            {
                var skipButtonRect = new Rectangle(balloonRect.Right - 140, buttonY, 60, 30);
                DrawButton(g, skipButtonRect, "跳过", foregroundColor);
                _skipButtonRect = skipButtonRect;
            }
            else
            {
                _skipButtonRect = Rectangle.Empty;
            }
        }

        /// <summary>
        /// 绘制单个按钮
        /// </summary>
        /// <param name="g">绘图对象</param>
        /// <param name="rect">按钮矩形</param>
        /// <param name="text">按钮文本</param>
        /// <param name="color">按钮颜色</param>
        private void DrawButton(Graphics g, Rectangle rect, string text, Color color)
        {
            // 绘制按钮背景
            using (var brush = new SolidBrush(Color.FromArgb(50, color)))
            {
                g.FillRectangle(brush, rect);
            }

            // 绘制按钮边框
            using (var pen = new Pen(color, 1))
            {
                g.DrawRectangle(pen, rect);
            }

            // 绘制按钮文本
            using (var font = new Font("Microsoft YaHei", 9f))
            using (var textBrush = new SolidBrush(color))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(text, font, textBrush, rect, format);
            }
        }

        /// <summary>
        /// 绘制进度指示器
        /// </summary>
        /// <param name="g">绘图对象</param>
        private void DrawProgressIndicator(Graphics g)
        {
            if (_guideSteps.Count <= 1) return;

            var accentColor = GetThemeColor(GuideThemeColor.Accent);
            var progressY = ClientRectangle.Bottom - 30;
            var progressWidth = Math.Min(300, ClientRectangle.Width - 40);
            var progressX = (ClientRectangle.Width - progressWidth) / 2;

            // 绘制进度条背景
            var progressRect = new Rectangle(progressX, progressY, progressWidth, 6);
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
            {
                g.FillRectangle(backgroundBrush, progressRect);
            }

            // 绘制进度条
            var progress = (float)(_currentStepIndex + 1) / _guideSteps.Count;
            var fillWidth = (int)(progressWidth * progress);
            var fillRect = new Rectangle(progressX, progressY, fillWidth, 6);
            using (var fillBrush = new SolidBrush(accentColor))
            {
                g.FillRectangle(fillBrush, fillRect);
            }

            // 绘制进度文本
            var progressText = $"{_currentStepIndex + 1} / {_guideSteps.Count}";
            using (var font = new Font("Microsoft YaHei", 9f))
            using (var textBrush = new SolidBrush(Color.White))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                var textRect = new Rectangle(progressX, progressY - 20, progressWidth, 15);
                g.DrawString(progressText, font, textBrush, textRect, format);
            }
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 动画定时器事件处理
        /// </summary>
        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            if (!_isAnimating) return;

            _animationProgress += 0.05f;
            if (_animationProgress >= 1f)
            {
                _animationProgress = 1f;
                _isAnimating = false;
                _animationTimer.Stop();
            }

            Invalidate();
        }

        /// <summary>
        /// 自动前进定时器事件处理
        /// </summary>
        private void OnAutoAdvanceTimerTick(object sender, EventArgs e)
        {
            _autoAdvanceTimer.Stop();
            NextStep();
        }

        /// <summary>
        /// 鼠标点击事件处理
        /// </summary>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (!_isInteractiveMode) return;

            var clickPoint = e.Location;

            // 检查按钮点击
            if (_nextButtonRect.Contains(clickPoint))
            {
                NextStep();
                OnUserInteraction("NextButton");
            }
            else if (_prevButtonRect.Contains(clickPoint))
            {
                PreviousStep();
                OnUserInteraction("PreviousButton");
            }
            else if (_skipButtonRect.Contains(clickPoint))
            {
                SkipGuide();
                OnUserInteraction("SkipButton");
            }
        }

        /// <summary>
        /// 键盘按键事件处理
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!_isInteractiveMode) return;

            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Space:
                    NextStep();
                    OnUserInteraction("KeyNext");
                    break;
                case Keys.Left:
                case Keys.Up:
                    PreviousStep();
                    OnUserInteraction("KeyPrevious");
                    break;
                case Keys.Escape:
                    if (_currentStep?.AllowSkip == true)
                    {
                        SkipGuide();
                        OnUserInteraction("KeySkip");
                    }
                    break;
            }
        }

        #endregion

        #region 事件触发方法

        /// <summary>
        /// 触发引导开始事件
        /// </summary>
        protected virtual void OnGuideStarted()
        {
            GuideStarted?.Invoke(this, new GuideStartedEventArgs
            {
                Title = _guideTitle,
                TotalSteps = _guideSteps.Count,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发步骤变化事件
        /// </summary>
        /// <param name="previousStep">上一个步骤</param>
        /// <param name="currentStep">当前步骤</param>
        protected virtual void OnStepChanged(GuideStep previousStep, GuideStep currentStep)
        {
            StepChanged?.Invoke(this, new GuideStepChangedEventArgs
            {
                PreviousStep = previousStep,
                CurrentStep = currentStep,
                StepIndex = _currentStepIndex,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发引导完成事件
        /// </summary>
        protected virtual void OnGuideCompleted()
        {
            GuideCompleted?.Invoke(this, new GuideCompletedEventArgs
            {
                Title = _guideTitle,
                CompletedSteps = _currentStepIndex + 1,
                TotalSteps = _guideSteps.Count,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发引导跳过事件
        /// </summary>
        protected virtual void OnGuideSkipped()
        {
            GuideSkipped?.Invoke(this, new GuideSkippedEventArgs
            {
                Title = _guideTitle,
                CurrentStep = _currentStepIndex + 1,
                TotalSteps = _guideSteps.Count,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发用户交互事件
        /// </summary>
        /// <param name="action">交互动作</param>
        protected virtual void OnUserInteraction(string action)
        {
            UserInteraction?.Invoke(this, new GuideInteractionEventArgs
            {
                Action = action,
                StepIndex = _currentStepIndex,
                StepTitle = _currentStep?.Title,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region 字段

        private Rectangle _prevButtonRect;
        private Rectangle _nextButtonRect;
        private Rectangle _skipButtonRect;

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 停止定时器
                _animationTimer?.Stop();
                _autoAdvanceTimer?.Stop();
                _animationTimer?.Dispose();
                _autoAdvanceTimer?.Dispose();

                // 释放背景缓冲
                _backBuffer?.Dispose();

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 引导步骤
    /// </summary>
    public class GuideStep
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public Rectangle HighlightRegion { get; set; }
        public Point BalloonPosition { get; set; }
        public bool ShowButtons { get; set; } = true;
        public bool AllowSkip { get; set; } = true;
        public bool ShowFinishButton { get; set; }
        public bool AutoAdvance { get; set; }
        public int? AutoAdvanceDelay { get; set; }
        public string[] Keywords { get; set; }
        public object Tag { get; set; }
    }

    /// <summary>
    /// 引导主题
    /// </summary>
    public enum GuideTheme
    {
        Default,
        Dark,
        Light,
        Blue,
        Green,
        Custom
    }

    /// <summary>
    /// 引导主题颜色
    /// </summary>
    public enum GuideThemeColor
    {
        Background,
        Foreground,
        Accent,
        Border
    }

    /// <summary>
    /// 引导开始事件参数
    /// </summary>
    public class GuideStartedEventArgs : EventArgs
    {
        public string Title { get; set; }
        public int TotalSteps { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 引导步骤变化事件参数
    /// </summary>
    public class GuideStepChangedEventArgs : EventArgs
    {
        public GuideStep PreviousStep { get; set; }
        public GuideStep CurrentStep { get; set; }
        public int StepIndex { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 引导完成事件参数
    /// </summary>
    public class GuideCompletedEventArgs : EventArgs
    {
        public string Title { get; set; }
        public int CompletedSteps { get; set; }
        public int TotalSteps { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 引导跳过事件参数
    /// </summary>
    public class GuideSkippedEventArgs : EventArgs
    {
        public string Title { get; set; }
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 引导用户交互事件参数
    /// </summary>
    public class GuideInteractionEventArgs : EventArgs
    {
        public string Action { get; set; }
        public int StepIndex { get; set; }
        public string StepTitle { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}