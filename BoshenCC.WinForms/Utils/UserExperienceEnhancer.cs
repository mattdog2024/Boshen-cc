using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 用户体验增强器
    /// 提供工具提示、动画效果、用户反馈等用户体验增强功能
    /// </summary>
    public class UserExperienceEnhancer : IDisposable
    {
        #region 私有字段

        private readonly Dictionary<Control, DateTime> _lastInteractionTimes;
        private readonly Dictionary<Control, Point> _lastMousePositions;
        private readonly Timer _idleTimer;
        private readonly Timer _animationTimer;
        private readonly List<AnimationInfo> _activeAnimations;

        private Control _ownerControl;
        private bool _disposed;
        private bool _isUserIdle;
        private DateTime _lastActivityTime;
        private int _currentTheme;
        private double _currentDpiScale;

        #endregion

        #region 事件定义

        /// <summary>
        /// 用户状态变化事件
        /// </summary>
        public event EventHandler<UserStateEventArgs> UserStateChanged;

        /// <summary>
        /// 交互提示事件
        /// </summary>
        public event EventHandler<InteractionHintEventArgs> InteractionHintRequested;

        /// <summary>
        /// 动画完成事件
        /// </summary>
        public event EventHandler<AnimationCompletedEventArgs> AnimationCompleted;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化UserExperienceEnhancer类
        /// </summary>
        /// <param name="owner">拥有者控件</param>
        public UserExperienceEnhancer(Control owner)
        {
            _ownerControl = owner ?? throw new ArgumentNullException(nameof(owner));

            _lastInteractionTimes = new Dictionary<Control, DateTime>();
            _lastMousePositions = new Dictionary<Control, Point>();
            _activeAnimations = new List<AnimationInfo>();

            InitializeTimers();
            InitializeSettings();

            _currentDpiScale = DPIHelper.GetDpiScaleFactor(owner);
            _lastActivityTime = DateTime.Now;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取或设置当前主题
        /// </summary>
        public int CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnThemeChanged();
                }
            }
        }

        /// <summary>
        /// 获取或设置用户是否空闲
        /// </summary>
        public bool IsUserIdle
        {
            get => _isUserIdle;
            private set
            {
                if (_isUserIdle != value)
                {
                    _isUserIdle = value;
                    OnUserStateChanged();
                }
            }
        }

        /// <summary>
        /// 获取当前DPI缩放比例
        /// </summary>
        public double CurrentDpiScale => _currentDpiScale;

        /// <summary>
        /// 获取活跃动画数量
        /// </summary>
        public int ActiveAnimationCount => _activeAnimations.Count;

        #endregion

        #region 公共方法

        /// <summary>
        /// 注册控件的交互跟踪
        /// </summary>
        /// <param name="control">要注册的控件</param>
        public void RegisterControl(Control control)
        {
            if (control == null) return;

            if (!_lastInteractionTimes.ContainsKey(control))
            {
                _lastInteractionTimes[control] = DateTime.Now;
                _lastMousePositions[control] = Point.Empty;

                // 订阅控件事件
                control.MouseEnter += OnControlMouseEnter;
                control.MouseLeave += OnControlMouseLeave;
                control.MouseMove += OnControlMouseMove;
                control.Click += OnControlClick;
                control.GotFocus += OnControlGotFocus;
                control.LostFocus += OnControlLostFocus;
            }
        }

        /// <summary>
        /// 取消注册控件的交互跟踪
        /// </summary>
        /// <param name="control">要取消注册的控件</param>
        public void UnregisterControl(Control control)
        {
            if (control == null || !_lastInteractionTimes.ContainsKey(control)) return;

            // 取消订阅控件事件
            control.MouseEnter -= OnControlMouseEnter;
            control.MouseLeave -= OnControlMouseLeave;
            control.MouseMove -= OnControlMouseMove;
            control.Click -= OnControlClick;
            control.GotFocus -= OnControlGotFocus;
            control.LostFocus -= OnControlLostFocus;

            _lastInteractionTimes.Remove(control);
            _lastMousePositions.Remove(control);
        }

        /// <summary>
        /// 创建智能工具提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="text">提示文本</param>
        /// <param name="title">提示标题</param>
        /// <param name="icon">提示图标</param>
        /// <param name="duration">显示持续时间（毫秒）</param>
        /// <returns>工具提示对象</returns>
        public SmartTooltip CreateSmartTooltip(Control control, string text, string title = null, ToolTipIcon icon = ToolTipIcon.Info, int duration = 3000)
        {
            if (control == null) return null;

            var tooltip = new SmartTooltip
            {
                ToolTipTitle = title,
                ToolTipIcon = icon,
                AutoPopDelay = duration,
                InitialDelay = GetIntelligentDelay(control),
                ReshowDelay = 100,
                UseAnimation = true,
                UseFading = true
            };

            tooltip.SetToolTip(control, text);
            return tooltip;
        }

        /// <summary>
        /// 显示上下文帮助提示
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="context">帮助上下文</param>
        public void ShowContextHelp(Control control, string context)
        {
            if (control == null || string.IsNullOrEmpty(context)) return;

            var helpText = GetHelpTextForContext(context);
            if (!string.IsNullOrEmpty(helpText))
            {
                CreateSmartTooltip(control, helpText, "帮助提示", ToolTipIcon.Info, 5000);
            }
        }

        /// <summary>
        /// 开始控件动画
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="animationType">动画类型</param>
        /// <param name="duration">动画持续时间</param>
        /// <param name="easing">动画缓动函数</param>
        public void StartAnimation(Control control, AnimationType animationType, int duration = 300, EasingType easing = EasingType.QuadOut)
        {
            if (control == null) return;

            var animation = new AnimationInfo
            {
                Control = control,
                Type = animationType,
                Duration = duration,
                Easing = easing,
                StartTime = DateTime.Now,
                StartPosition = control.Location,
                StartSize = control.Size,
                StartOpacity = control.Opacity
            };

            // 移除同一控件的旧动画
            _activeAnimations.RemoveAll(a => a.Control == control);
            _activeAnimations.Add(animation);

            if (!_animationTimer.Enabled)
            {
                _animationTimer.Start();
            }
        }

        /// <summary>
        /// 停止控件动画
        /// </summary>
        /// <param name="control">目标控件</param>
        public void StopAnimation(Control control)
        {
            if (control == null) return;

            var animations = _activeAnimations.Where(a => a.Control == control).ToList();
            foreach (var animation in animations)
            {
                _activeAnimations.Remove(animation);
                OnAnimationCompleted(animation);
            }

            if (_activeAnimations.Count == 0)
            {
                _animationTimer.Stop();
            }
        }

        /// <summary>
        /// 显示操作反馈
        /// </summary>
        /// <param name="message">反馈消息</param>
        /// <param name="type">反馈类型</param>
        /// <param name="duration">显示持续时间</param>
        public void ShowFeedback(string message, FeedbackType type = FeedbackType.Info, int duration = 2000)
        {
            if (string.IsNullOrEmpty(message)) return;

            var feedback = new FeedbackForm
            {
                Message = message,
                Type = type,
                Duration = duration,
                Theme = _currentTheme
            };

            // 在拥有者控件附近显示反馈
            var location = GetOptimalFeedbackLocation(feedback.Size);
            feedback.Location = location;
            feedback.ShowFeedback();
        }

        /// <summary>
        /// 获取控件的智能提示内容
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>提示内容</returns>
        public string GetIntelligentTooltipContent(Control control)
        {
            if (control == null) return string.Empty;

            var content = new StringBuilder();

            // 基础信息
            content.AppendLine($"控件: {control.Name}");
            content.AppendLine($"类型: {control.GetType().Name}");

            // 状态信息
            if (control.Enabled)
                content.AppendLine("状态: 已启用");
            else
                content.AppendLine("状态: 已禁用");

            // 交互历史
            if (_lastInteractionTimes.TryGetValue(control, out var lastTime))
            {
                var timeSince = DateTime.Now - lastTime;
                content.AppendLine($"上次交互: {FormatTimeSpan(timeSince)}");
            }

            // 快捷键信息
            var shortcuts = GetShortcutsForControl(control);
            if (!string.IsNullOrEmpty(shortcuts))
            {
                content.AppendLine($"快捷键: {shortcuts}");
            }

            return content.ToString().Trim();
        }

        /// <summary>
        /// 分析用户行为模式
        /// </summary>
        /// <returns>用户行为分析结果</returns>
        public UserBehaviorAnalysis AnalyzeUserBehavior()
        {
            var analysis = new UserBehaviorAnalysis();

            // 分析交互频率
            var interactionTimes = _lastInteractionTimes.Values.ToList();
            if (interactionTimes.Count > 1)
            {
                var intervals = new List<double>();
                for (int i = 1; i < interactionTimes.Count; i++)
                {
                    intervals.Add((interactionTimes[i] - interactionTimes[i-1]).TotalSeconds);
                }

                analysis.AverageInteractionInterval = intervals.Average();
                analysis.InteractionFrequency = 1.0 / analysis.AverageInteractionInterval;
            }

            // 分析常用控件
            analysis.MostUsedControls = _lastInteractionTimes
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key.Name)
                .ToList();

            // 分析会话时长
            analysis.SessionDuration = DateTime.Now - _lastActivityTime;

            return analysis;
        }

        /// <summary>
        /// 优化用户体验建议
        /// </summary>
        /// <returns>优化建议列表</returns>
        public List<string> GetOptimizationSuggestions()
        {
            var suggestions = new List<string>();
            var analysis = AnalyzeUserBehavior();

            // 基于交互频率的建议
            if (analysis.InteractionFrequency < 0.1) // 交互频率过低
            {
                suggestions.Add("考虑添加更多交互提示和引导");
            }
            else if (analysis.InteractionFrequency > 2.0) // 交互频率过高
            {
                suggestions.Add("考虑简化操作流程，减少不必要的点击");
            }

            // 基于会话时长的建议
            if (analysis.SessionDuration.TotalMinutes > 30)
            {
                suggestions.Add("长时间使用，建议添加休息提醒");
            }

            // 基于常用控件的建议
            if (analysis.MostUsedControls.Any())
            {
                suggestions.Add($"优化常用控件布局: {string.Join(", ", analysis.MostUsedControls.Take(3))}");
            }

            return suggestions;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitializeTimers()
        {
            _idleTimer = new Timer
            {
                Interval = 1000 // 每秒检查一次
            };
            _idleTimer.Tick += OnIdleTimerTick;
            _idleTimer.Start();

            _animationTimer = new Timer
            {
                Interval = 16 // ~60 FPS
            };
            _animationTimer.Tick += OnAnimationTimerTick;
        }

        /// <summary>
        /// 初始化设置
        /// </summary>
        private void InitializeSettings()
        {
            _currentTheme = 0; // 默认主题
            _isUserIdle = false;
            _currentDpiScale = 1.0;
        }

        /// <summary>
        /// 获取智能延迟时间
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>延迟时间（毫秒）</returns>
        private int GetIntelligentDelay(Control control)
        {
            if (_lastInteractionTimes.TryGetValue(control, out var lastTime))
            {
                var timeSince = DateTime.Now - lastTime;
                // 最近交互过的控件显示更快
                if (timeSince.TotalSeconds < 5)
                    return 500;
                else if (timeSince.TotalMinutes < 1)
                    return 1000;
            }
            return 1500; // 默认延迟
        }

        /// <summary>
        /// 根据上下文获取帮助文本
        /// </summary>
        /// <param name="context">帮助上下文</param>
        /// <returns>帮助文本</returns>
        private string GetHelpTextForContext(string context)
        {
            var helpTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KLineSelector"] = "K线选择器：点击图表上的K线来选择测量点。\n快捷键：1-4切换测量模式，C清除选择，U撤销，R重做。",
                ["PriceDisplay"] = "价格显示：显示当前选中K线的价格信息和计算结果。\n包括开高低收价格和波神算法预测结果。",
                ["SelectionPanel"] = "选择面板：管理测量模式和操作。\n提供清除选择、撤销重做、模式切换等功能。",
                ["MainWindow"] = "主窗口：波神K线测量工具的主界面。\n支持图片打开、测量计算、结果导出等功能。\n快捷键：Ctrl+O打开，Ctrl+S保存，F1显示帮助。"
            };

            return helpTexts.TryGetValue(context, out var text) ? text : string.Empty;
        }

        /// <summary>
        /// 获取控件关联的快捷键
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <returns>快捷键字符串</returns>
        private string GetShortcutsForControl(Control control)
        {
            var shortcuts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KLineSelector"] = "1-4, C, U, R, M",
                ["SelectionPanel"] = "Space, Ctrl+Z, Ctrl+Y",
                ["MainWindow"] = "Ctrl+O, Ctrl+S, F1, F11, ESC"
            };

            return shortcuts.TryGetValue(control.Name, out var shortcut) ? shortcut : string.Empty;
        }

        /// <summary>
        /// 获取最佳反馈显示位置
        /// </summary>
        /// <param name="feedbackSize">反馈窗体大小</param>
        /// <returns>显示位置</returns>
        private Point GetOptimalFeedbackLocation(Size feedbackSize)
        {
            var screenBounds = Screen.PrimaryScreen.WorkingArea;
            var ownerBounds = _ownerControl?.Bounds ?? Rectangle.Empty;

            // 默认显示在拥有者控件的右下方
            var x = ownerBounds.Right + 10;
            var y = ownerBounds.Bottom - feedbackSize.Height;

            // 确保不超出屏幕边界
            if (x + feedbackSize.Width > screenBounds.Right)
                x = screenBounds.Right - feedbackSize.Width - 10;
            if (y < screenBounds.Top)
                y = screenBounds.Top + 10;
            if (y + feedbackSize.Height > screenBounds.Bottom)
                y = screenBounds.Bottom - feedbackSize.Height - 10;

            return new Point(x, y);
        }

        /// <summary>
        /// 格式化时间跨度
        /// </summary>
        /// <param name="timeSpan">时间跨度</param>
        /// <returns>格式化的字符串</returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.TotalSeconds:F0}秒前";
            else if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.TotalMinutes:F0}分钟前";
            else
                return $"{timeSpan.TotalHours:F0}小时前";
        }

        /// <summary>
        /// 应用动画缓动函数
        /// </summary>
        /// <param name="t">进度值(0-1)</param>
        /// <param name="easing">缓动类型</param>
        /// <returns>缓动后的值</returns>
        private double ApplyEasing(double t, EasingType easing)
        {
            switch (easing)
            {
                case EasingType.Linear:
                    return t;
                case EasingType.QuadIn:
                    return t * t;
                case EasingType.QuadOut:
                    return 1 - (1 - t) * (1 - t);
                case EasingType.QuadInOut:
                    return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
                case EasingType.CubicIn:
                    return t * t * t;
                case EasingType.CubicOut:
                    return 1 - Math.Pow(1 - t, 3);
                case EasingType.CubicInOut:
                    return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
                default:
                    return t;
            }
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 空闲定时器事件处理
        /// </summary>
        private void OnIdleTimerTick(object sender, EventArgs e)
        {
            var timeSinceActivity = DateTime.Now - _lastActivityTime;
            var wasIdle = IsUserIdle;
            IsUserIdle = timeSinceActivity.TotalSeconds > 30; // 30秒无操作视为空闲

            if (wasIdle != IsUserIdle)
            {
                OnUserStateChanged();
            }
        }

        /// <summary>
        /// 动画定时器事件处理
        /// </summary>
        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var completedAnimations = new List<AnimationInfo>();

            foreach (var animation in _activeAnimations)
            {
                var elapsed = (now - animation.StartTime).TotalMilliseconds;
                var progress = Math.Min(1.0, elapsed / animation.Duration);
                var easedProgress = ApplyEasing(progress, animation.Easing);

                try
                {
                    ApplyAnimationFrame(animation, easedProgress);

                    if (progress >= 1.0)
                    {
                        completedAnimations.Add(animation);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但继续其他动画
                    System.Diagnostics.Debug.WriteLine($"动画应用错误: {ex.Message}");
                    completedAnimations.Add(animation);
                }
            }

            // 移除完成的动画
            foreach (var animation in completedAnimations)
            {
                _activeAnimations.Remove(animation);
                OnAnimationCompleted(animation);
            }

            // 如果没有活跃动画，停止定时器
            if (_activeAnimations.Count == 0)
            {
                _animationTimer.Stop();
            }
        }

        /// <summary>
        /// 应用动画帧
        /// </summary>
        /// <param name="animation">动画信息</param>
        /// <param name="progress">动画进度</param>
        private void ApplyAnimationFrame(AnimationInfo animation, double progress)
        {
            if (animation.Control == null || animation.Control.IsDisposed) return;

            switch (animation.Type)
            {
                case AnimationType.FadeIn:
                    animation.Control.Opacity = animation.StartOpacity + (1.0 - animation.StartOpacity) * progress;
                    break;

                case AnimationType.FadeOut:
                    animation.Control.Opacity = animation.StartOpacity * (1.0 - progress);
                    break;

                case AnimationType.SlideIn:
                    var targetX = animation.StartPosition.X;
                    var startX = targetX - 100;
                    animation.Control.Left = (int)(startX + (targetX - startX) * progress);
                    break;

                case AnimationType.Bounce:
                    var bounceHeight = Math.Sin(progress * Math.PI) * 10;
                    animation.Control.Top = animation.StartPosition.Y - (int)bounceHeight;
                    break;

                case AnimationType.Scale:
                    var scale = 1.0 + Math.Sin(progress * Math.PI) * 0.1;
                    var newWidth = (int)(animation.StartSize.Width * scale);
                    var newHeight = (int)(animation.StartSize.Height * scale);
                    animation.Control.Size = new Size(newWidth, newHeight);
                    break;
            }
        }

        /// <summary>
        /// 控件鼠标进入事件处理
        /// </summary>
        private void OnControlMouseEnter(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                _lastInteractionTimes[control] = DateTime.Now;
                UpdateLastActivityTime();

                // 触发交互提示事件
                OnInteractionHintRequested(control, "鼠标进入");
            }
        }

        /// <summary>
        /// 控件鼠标离开事件处理
        /// </summary>
        private void OnControlMouseLeave(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                UpdateLastActivityTime();
            }
        }

        /// <summary>
        /// 控件鼠标移动事件处理
        /// </summary>
        private void OnControlMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Control control)
            {
                _lastMousePositions[control] = e.Location;
                UpdateLastActivityTime();
            }
        }

        /// <summary>
        /// 控件点击事件处理
        /// </summary>
        private void OnControlClick(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                _lastInteractionTimes[control] = DateTime.Now;
                UpdateLastActivityTime();

                // 添加点击反馈动画
                StartAnimation(control, AnimationType.Bounce, 200);

                // 触发交互提示事件
                OnInteractionHintRequested(control, "点击操作");
            }
        }

        /// <summary>
        /// 控件获得焦点事件处理
        /// </summary>
        private void OnControlGotFocus(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                UpdateLastActivityTime();

                // 添加焦点反馈动画
                StartAnimation(control, AnimationType.Scale, 150);
            }
        }

        /// <summary>
        /// 控件失去焦点事件处理
        /// </summary>
        private void OnControlLostFocus(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                UpdateLastActivityTime();
            }
        }

        /// <summary>
        /// 更新最后活动时间
        /// </summary>
        private void UpdateLastActivityTime()
        {
            _lastActivityTime = DateTime.Now;
            if (IsUserIdle)
            {
                IsUserIdle = false;
            }
        }

        #endregion

        #region 事件触发方法

        /// <summary>
        /// 触发用户状态变化事件
        /// </summary>
        protected virtual void OnUserStateChanged()
        {
            UserStateChanged?.Invoke(this, new UserStateEventArgs
            {
                IsIdle = IsUserIdle,
                LastActivityTime = _lastActivityTime,
                SessionDuration = DateTime.Now - _lastActivityTime
            });
        }

        /// <summary>
        /// 触发交互提示请求事件
        /// </summary>
        /// <param name="control">相关控件</param>
        /// <param name="hint">提示内容</param>
        protected virtual void OnInteractionHintRequested(Control control, string hint)
        {
            InteractionHintRequested?.Invoke(this, new InteractionHintEventArgs
            {
                Control = control,
                Hint = hint,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发动画完成事件
        /// </summary>
        /// <param name="animation">完成的动画</param>
        protected virtual void OnAnimationCompleted(AnimationInfo animation)
        {
            AnimationCompleted?.Invoke(this, new AnimationCompletedEventArgs
            {
                Control = animation.Control,
                AnimationType = animation.Type,
                Duration = animation.Duration
            });
        }

        /// <summary>
        /// 主题变化处理
        /// </summary>
        protected virtual void OnThemeChanged()
        {
            // 主题变化时可以更新相关UI元素
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
                // 停止定时器
                _idleTimer?.Stop();
                _animationTimer?.Stop();

                // 释放定时器
                _idleTimer?.Dispose();
                _animationTimer?.Dispose();

                // 清理资源
                _lastInteractionTimes.Clear();
                _lastMousePositions.Clear();
                _activeAnimations.Clear();

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 智能工具提示类
    /// </summary>
    public class SmartTooltip : ToolTip
    {
        public bool UseAnimation { get; set; } = true;
        public bool UseFading { get; set; } = true;
    }

    /// <summary>
    /// 动画信息类
    /// </summary>
    public class AnimationInfo
    {
        public Control Control { get; set; }
        public AnimationType Type { get; set; }
        public int Duration { get; set; }
        public EasingType Easing { get; set; }
        public DateTime StartTime { get; set; }
        public Point StartPosition { get; set; }
        public Size StartSize { get; set; }
        public double StartOpacity { get; set; }
    }

    /// <summary>
    /// 用户行为分析结果
    /// </summary>
    public class UserBehaviorAnalysis
    {
        public double AverageInteractionInterval { get; set; }
        public double InteractionFrequency { get; set; }
        public List<string> MostUsedControls { get; set; } = new List<string>();
        public TimeSpan SessionDuration { get; set; }
    }

    /// <summary>
    /// 用户状态事件参数
    /// </summary>
    public class UserStateEventArgs : EventArgs
    {
        public bool IsIdle { get; set; }
        public DateTime LastActivityTime { get; set; }
        public TimeSpan SessionDuration { get; set; }
    }

    /// <summary>
    /// 交互提示事件参数
    /// </summary>
    public class InteractionHintEventArgs : EventArgs
    {
        public Control Control { get; set; }
        public string Hint { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 动画完成事件参数
    /// </summary>
    public class AnimationCompletedEventArgs : EventArgs
    {
        public Control Control { get; set; }
        public AnimationType AnimationType { get; set; }
        public int Duration { get; set; }
    }

    /// <summary>
    /// 反馈窗体
    /// </summary>
    public class FeedbackForm : Form
    {
        public string Message { get; set; }
        public FeedbackType Type { get; set; }
        public int Duration { get; set; }
        public int Theme { get; set; }

        public void ShowFeedback()
        {
            // 实现反馈显示逻辑
            // 这里简化处理，实际应该创建一个独立的窗体显示反馈
        }
    }

    /// <summary>
    /// 动画类型枚举
    /// </summary>
    public enum AnimationType
    {
        FadeIn,
        FadeOut,
        SlideIn,
        Bounce,
        Scale
    }

    /// <summary>
    /// 缓动类型枚举
    /// </summary>
    public enum EasingType
    {
        Linear,
        QuadIn,
        QuadOut,
        QuadInOut,
        CubicIn,
        CubicOut,
        CubicInOut
    }

    /// <summary>
    /// 反馈类型枚举
    /// </summary>
    public enum FeedbackType
    {
        Info,
        Success,
        Warning,
        Error
    }

    #endregion
}