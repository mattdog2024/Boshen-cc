using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoshenCC.WinForms.Controls;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 交互增强工具类
    /// 提供高级交互功能，包括增强的十字准星、鼠标状态管理、键盘快捷键处理等
    /// </summary>
    public class InteractionEnhancer : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        #region 私有字段

        private readonly Control _targetControl;
        private readonly List<KeyboardShortcut> _shortcuts;
        private readonly MouseStateManager _mouseStateManager;
        private readonly CrosshairRenderer _crosshairRenderer;
        private readonly TooltipManager _tooltipManager;
        private readonly AnimationManager _animationManager;
        private bool _isEnhancedMode;
        private bool _showEnhancedCrosshair;
        private bool _enableMouseTracking;
        private bool _enableKeyboardShortcuts;
        private Cursor _originalCursor;
        private Point _lastMousePosition;
        private DateTime _lastMouseMoveTime;
        private readonly Timer _animationTimer;

        #endregion

        #region 构造函数

        public InteractionEnhancer(Control targetControl)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));

            _shortcuts = new List<KeyboardShortcut>();
            _mouseStateManager = new MouseStateManager();
            _crosshairRenderer = new CrosshairRenderer();
            _tooltipManager = new TooltipManager();
            _animationManager = new AnimationManager();

            _isEnhancedMode = false;
            _showEnhancedCrosshair = true;
            _enableMouseTracking = true;
            _enableKeyboardShortcuts = true;
            _lastMousePosition = Point.Empty;
            _lastMouseMoveTime = DateTime.Now;

            _animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;

            InitializeEventHandlers();
            InitializeDefaultShortcuts();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否启用增强模式
        /// </summary>
        public bool IsEnhancedMode
        {
            get => _isEnhancedMode;
            set
            {
                if (_isEnhancedMode != value)
                {
                    _isEnhancedMode = value;
                    OnEnhancedModeChanged(value);
                }
            }
        }

        /// <summary>
        /// 是否显示增强十字准星
        /// </summary>
        public bool ShowEnhancedCrosshair
        {
            get => _showEnhancedCrosshair;
            set => _showEnhancedCrosshair = value;
        }

        /// <summary>
        /// 是否启用鼠标跟踪
        /// </summary>
        public bool EnableMouseTracking
        {
            get => _enableMouseTracking;
            set => _enableMouseTracking = value;
        }

        /// <summary>
        /// 是否启用键盘快捷键
        /// </summary>
        public bool EnableKeyboardShortcuts
        {
            get => _enableKeyboardShortcuts;
            set => _enableKeyboardShortcuts = value;
        }

        /// <summary>
        /// 十字准星渲染器
        /// </summary>
        public CrosshairRenderer CrosshairRenderer => _crosshairRenderer;

        /// <summary>
        /// 鼠标状态管理器
        /// </summary>
        public MouseStateManager MouseStateManager => _mouseStateManager;

        /// <summary>
        /// 工具提示管理器
        /// </summary>
        public TooltipManager TooltipManager => _tooltipManager;

        /// <summary>
        /// 动画管理器
        /// </summary>
        public AnimationManager AnimationManager => _animationManager;

        #endregion

        #region 事件

        /// <summary>
        /// 增强模式改变事件
        /// </summary>
        public event EventHandler<bool> EnhancedModeChanged;

        /// <summary>
        /// 快捷键触发事件
        /// </summary>
        public event EventHandler<ShortcutTriggeredEventArgs> ShortcutTriggered;

        /// <summary>
        /// 鼠标活动事件
        /// </summary>
        public event EventHandler<MouseActivityEventArgs> MouseActivity;

        /// <summary>
        /// 十字准星位置改变事件
        /// </summary>
        public event EventHandler<CrosshairPositionChangedEventArgs> CrosshairPositionChanged;

        #endregion

        #region 公共方法

        /// <summary>
        /// 启用增强功能
        /// </summary>
        public void Enable()
        {
            IsEnhancedMode = true;
            _animationTimer.Start();
        }

        /// <summary>
        /// 禁用增强功能
        /// </summary>
        public void Disable()
        {
            IsEnhancedMode = false;
            _animationTimer.Stop();
        }

        /// <summary>
        /// 添加键盘快捷键
        /// </summary>
        /// <param name="shortcut">快捷键</param>
        public void AddShortcut(KeyboardShortcut shortcut)
        {
            if (shortcut != null && !_shortcuts.Contains(shortcut))
            {
                _shortcuts.Add(shortcut);
            }
        }

        /// <summary>
        /// 移除键盘快捷键
        /// </summary>
        /// <param name="shortcut">快捷键</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveShortcut(KeyboardShortcut shortcut)
        {
            return _shortcuts.Remove(shortcut);
        }

        /// <summary>
        /// 清除所有快捷键
        /// </summary>
        public void ClearShortcuts()
        {
            _shortcuts.Clear();
        }

        /// <summary>
        /// 设置十字准星样式
        /// </summary>
        /// <param name="style">十字准星样式</param>
        public void SetCrosshairStyle(CrosshairStyle style)
        {
            _crosshairRenderer.SetStyle(style);
        }

        /// <summary>
        /// 移动十字准星到指定位置
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="animated">是否使用动画</param>
        public void MoveCrosshairTo(Point position, bool animated = true)
        {
            if (animated && _isEnhancedMode)
            {
                _animationManager.AnimateCrosshair(_crosshairRenderer.Position, position);
            }
            else
            {
                _crosshairRenderer.Position = position;
                OnCrosshairPositionChanged(position);
            }
        }

        /// <summary>
        /// 显示工具提示
        /// </summary>
        /// <param name="text">提示文本</param>
        /// <param name="position">位置</param>
        /// <param name="duration">持续时间（毫秒）</param>
        public void ShowTooltip(string text, Point position, int duration = 2000)
        {
            _tooltipManager.ShowTooltip(text, position, duration);
        }

        /// <summary>
        /// 隐藏工具提示
        /// </summary>
        public void HideTooltip()
        {
            _tooltipManager.HideTooltip();
        }

        /// <summary>
        /// 开始动画效果
        /// </summary>
        /// <param name="animationType">动画类型</param>
        /// <param name="duration">持续时间</param>
        public void StartAnimation(AnimationType animationType, int duration = 500)
        {
            _animationManager.StartAnimation(animationType, duration);
        }

        /// <summary>
        /// 停止所有动画
        /// </summary>
        public void StopAllAnimations()
        {
            _animationManager.StopAllAnimations();
        }

        /// <summary>
        /// 在指定控件上绘制增强效果
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="control">控件</param>
        public void DrawEnhancements(Graphics graphics, Control control)
        {
            if (!_isEnhancedMode) return;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制十字准星
            if (_showEnhancedCrosshair && _mouseStateManager.IsMouseOverControl)
            {
                _crosshairRenderer.Draw(graphics, control.ClientRectangle);
            }

            // 绘制工具提示
            _tooltipManager.Draw(graphics);

            // 绘制动画效果
            _animationManager.Draw(graphics, control.ClientRectangle);
        }

        /// <summary>
        /// 模拟鼠标点击
        /// </summary>
        /// <param name="position">点击位置</param>
        /// <param name="button">鼠标按钮</param>
        public void SimulateClick(Point position, MouseButtons button = MouseButtons.Left)
        {
            var oldPos = Cursor.Position;
            Cursor.Position = _targetControl.PointToScreen(position);

            // 发送鼠标消息
            var msg = button == MouseButtons.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            SendMessage(_targetControl.Handle, msg, 1, MakeLParam(position.X, position.Y));

            msg = button == MouseButtons.Left ? WM_LBUTTONUP : WM_RBUTTONUP;
            SendMessage(_targetControl.Handle, msg, 0, MakeLParam(position.X, position.Y));

            Cursor.Position = oldPos;
        }

        /// <summary>
        /// 设置鼠标位置
        /// </summary>
        /// <param name="position">新位置</param>
        public void SetMousePosition(Point position)
        {
            var screenPosition = _targetControl.PointToScreen(position);
            SetCursorPos(screenPosition.X, screenPosition.Y);
        }

        #endregion

        #region 私有方法

        private void InitializeEventHandlers()
        {
            _targetControl.MouseMove += TargetControl_MouseMove;
            _targetControl.MouseEnter += TargetControl_MouseEnter;
            _targetControl.MouseLeave += TargetControl_MouseLeave;
            _targetControl.KeyDown += TargetControl_KeyDown;
            _targetControl.KeyPress += TargetControl_KeyPress;
        }

        private void InitializeDefaultShortcuts()
        {
            // 常用快捷键
            AddShortcut(new KeyboardShortcut(Keys.Escape, "取消当前操作", () => OnShortcutTriggered("Escape")));
            AddShortcut(new KeyboardShortcut(Keys.F1, "显示帮助", () => OnShortcutTriggered("Help")));
            AddShortcut(new KeyboardShortcut(Keys.F5, "刷新", () => OnShortcutTriggered("Refresh")));
            AddShortcut(new KeyboardShortcut(Keys.Delete, "删除", () => OnShortcutTriggered("Delete")));

            // Ctrl组合键
            AddShortcut(new KeyboardShortcut(Keys.Control | Keys.Z, "撤销", () => OnShortcutTriggered("Undo")));
            AddShortcut(new KeyboardShortcut(Keys.Control | Keys.Y, "重做", () => OnShortcutTriggered("Redo")));
            AddShortcut(new KeyboardShortcut(Keys.Control | Keys.C, "复制", () => OnShortcutTriggered("Copy")));
            AddShortcut(new KeyboardShortcut(Keys.Control | Keys.V, "粘贴", () => OnShortcutTriggered("Paste")));

            // 数字键切换测量模式
            AddShortcut(new KeyboardShortcut(Keys.D1, "标准测量模式", () => OnShortcutTriggered("Mode1")));
            AddShortcut(new KeyboardShortcut(Keys.D2, "上影线测量模式", () => OnShortcutTriggered("Mode2")));
            AddShortcut(new KeyboardShortcut(Keys.D3, "下影线测量模式", () => OnShortcutTriggered("Mode3")));
            AddShortcut(new KeyboardShortcut(Keys.D4, "完整影线测量模式", () => OnShortcutTriggered("Mode4")));
        }

        private void TargetControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_enableMouseTracking) return;

            var currentTime = DateTime.Now;
            var timeDiff = (currentTime - _lastMouseMoveTime).TotalMilliseconds;

            _mouseStateManager.UpdateMousePosition(e.Location, _targetControl.ClientRectangle);
            _crosshairRenderer.Position = e.Location;

            if (timeDiff > 16) // 限制更新频率到~60 FPS
            {
                _lastMousePosition = e.Location;
                _lastMouseMoveTime = currentTime;
                OnMouseActivity(new MouseActivityEventArgs(e.Location, timeDiff));
            }

            _targetControl.Invalidate();
        }

        private void TargetControl_MouseEnter(object sender, EventArgs e)
        {
            _mouseStateManager.SetMouseOverControl(true);
            if (_isEnhancedMode)
            {
                _originalCursor = _targetControl.Cursor;
                _targetControl.Cursor = Cursors.Cross;
            }
        }

        private void TargetControl_MouseLeave(object sender, EventArgs e)
        {
            _mouseStateManager.SetMouseOverControl(false);
            _tooltipManager.HideTooltip();
            if (_originalCursor != null)
            {
                _targetControl.Cursor = _originalCursor;
            }
        }

        private void TargetControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_enableKeyboardShortcuts) return;

            var matchingShortcut = _shortcuts.FirstOrDefault(s => s.Matches(e.KeyCode, e.Modifiers));
            if (matchingShortcut != null)
            {
                e.Handled = true;
                matchingShortcut.Action?.Invoke();
            }
        }

        private void TargetControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 处理字符快捷键
            var charShortcut = _shortcuts.FirstOrDefault(s => s.Matches(e.KeyChar));
            charShortcut?.Action?.Invoke();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_animationManager.Update())
            {
                _targetControl.Invalidate();
            }
        }

        private void OnEnhancedModeChanged(bool enabled)
        {
            EnhancedModeChanged?.Invoke(this, enabled);

            if (enabled)
            {
                _targetControl.Cursor = Cursors.Cross;
            }
            else if (_originalCursor != null)
            {
                _targetControl.Cursor = _originalCursor;
            }
        }

        private void OnShortcutTriggered(string shortcutName)
        {
            ShortcutTriggered?.Invoke(this, new ShortcutTriggeredEventArgs(shortcutName));
        }

        private void OnMouseActivity(MouseActivityEventArgs e)
        {
            MouseActivity?.Invoke(this, e);
        }

        private void OnCrosshairPositionChanged(Point position)
        {
            CrosshairPositionChanged?.Invoke(this, new CrosshairPositionChangedEventArgs(position));
        }

        #endregion

        #region Win32 辅助方法

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        private static int MakeLParam(int x, int y)
        {
            return (y << 16) | x;
        }

        #endregion

        #region 资源释放

        public void Dispose()
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
            _crosshairRenderer?.Dispose();
            _tooltipManager?.Dispose();
            _animationManager?.Dispose();
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 键盘快捷键
    /// </summary>
    public class KeyboardShortcut
    {
        public Keys Keys { get; }
        public string Description { get; }
        public Action Action { get; }

        public KeyboardShortcut(Keys keys, string description, Action action)
        {
            Keys = keys;
            Description = description;
            Action = action;
        }

        public bool Matches(Keys keyCode, Keys modifiers)
        {
            return (Keys & Keys.KeyCode) == keyCode && (Keys & Keys.Modifiers) == modifiers;
        }

        public bool Matches(char keyChar)
        {
            return Keys == Keys.None && Description.Contains(keyChar.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 鼠标状态管理器
    /// </summary>
    public class MouseStateManager
    {
        public Point CurrentPosition { get; private set; }
        public bool IsMouseOverControl { get; private set; }
        public bool IsDragging { get; private set; }
        public DateTime LastActivity { get; private set; }

        public void UpdateMousePosition(Point position, Rectangle controlBounds)
        {
            CurrentPosition = position;
            LastActivity = DateTime.Now;
            IsMouseOverControl = controlBounds.Contains(position);
        }

        public void SetMouseOverControl(bool isOver)
        {
            IsMouseOverControl = isOver;
        }

        public void SetDragging(bool isDragging)
        {
            IsDragging = isDragging;
        }
    }

    /// <summary>
    /// 十字准星渲染器
    /// </summary>
    public class CrosshairRenderer : IDisposable
    {
        private Point _position;
        private CrosshairStyle _style;
        private Color _color;
        private int _thickness;
        private int _length;
        private float _opacity;
        private Pen _pen;
        private SolidBrush _brush;

        public Point Position
        {
            get => _position;
            set => _position = value;
        }

        public CrosshairRenderer()
        {
            _style = CrosshairStyle.Simple;
            _color = Color.Gray;
            _thickness = 1;
            _length = 20;
            _opacity = 1.0f;

            _pen = new Pen(_color, _thickness);
            _brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), _color));
        }

        public void SetStyle(CrosshairStyle style)
        {
            _style = style;
        }

        public void SetColor(Color color)
        {
            _color = color;
            UpdatePen();
        }

        public void SetOpacity(float opacity)
        {
            _opacity = Math.Max(0, Math.Min(1, opacity));
            UpdateBrush();
        }

        public void Draw(Graphics g, Rectangle bounds)
        {
            if (!bounds.Contains(_position)) return;

            switch (_style)
            {
                case CrosshairStyle.Simple:
                    DrawSimpleCrosshair(g, bounds);
                    break;
                case CrosshairStyle.Full:
                    DrawFullCrosshair(g, bounds);
                    break;
                case CrosshairStyle.Dashed:
                    DrawDashedCrosshair(g, bounds);
                    break;
                case CrosshairStyle.Circle:
                    DrawCircleCrosshair(g, bounds);
                    break;
            }
        }

        private void DrawSimpleCrosshair(Graphics g, Rectangle bounds)
        {
            // 水平线
            g.DrawLine(_pen, _position.X - _length, _position.Y, _position.X + _length, _position.Y);
            // 垂直线
            g.DrawLine(_pen, _position.X, _position.Y - _length, _position.X, _position.Y + _length);
        }

        private void DrawFullCrosshair(Graphics g, Rectangle bounds)
        {
            // 水平线贯穿整个控件
            g.DrawLine(_pen, bounds.Left, _position.Y, bounds.Right, _position.Y);
            // 垂直线贯穿整个控件
            g.DrawLine(_pen, _position.X, bounds.Top, _position.X, bounds.Bottom);
        }

        private void DrawDashedCrosshair(Graphics g, Rectangle bounds)
        {
            _pen.DashStyle = DashStyle.Dash;
            DrawFullCrosshair(g, bounds);
            _pen.DashStyle = DashStyle.Solid;
        }

        private void DrawCircleCrosshair(Graphics g, Rectangle bounds)
        {
            const int radius = 15;
            var rect = new Rectangle(_position.X - radius, _position.Y - radius, radius * 2, radius * 2);

            g.DrawEllipse(_pen, rect);
            // 绘制十字中心
            g.DrawLine(_pen, _position.X - 5, _position.Y, _position.X + 5, _position.Y);
            g.DrawLine(_pen, _position.X, _position.Y - 5, _position.X, _position.Y + 5);
        }

        private void UpdatePen()
        {
            _pen?.Dispose();
            _pen = new Pen(Color.FromArgb((int)(255 * _opacity), _color), _thickness);
        }

        private void UpdateBrush()
        {
            _brush?.Dispose();
            _brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), _color));
        }

        public void Dispose()
        {
            _pen?.Dispose();
            _brush?.Dispose();
        }
    }

    /// <summary>
    /// 十字准星样式
    /// </summary>
    public enum CrosshairStyle
    {
        Simple,    // 简单十字线
        Full,      // 全屏十字线
        Dashed,    // 虚线十字线
        Circle     // 圆形十字线
    }

    /// <summary>
    /// 工具提示管理器
    /// </summary>
    public class TooltipManager : IDisposable
    {
        private struct TooltipInfo
        {
            public string Text;
            public Point Position;
            public DateTime StartTime;
            public int Duration;
            public float Opacity;
        }

        private TooltipInfo? _currentTooltip;
        private Font _font;
        private SolidBrush _backgroundBrush;
        private SolidBrush _textBrush;
        private Pen _borderPen;

        public TooltipManager()
        {
            _font = new Font("Arial", 9);
            _backgroundBrush = new SolidBrush(Color.FromArgb(240, 255, 255));
            _textBrush = new SolidBrush(Color.Black);
            _borderPen = new Pen(Color.Gray);
        }

        public void ShowTooltip(string text, Point position, int duration = 2000)
        {
            _currentTooltip = new TooltipInfo
            {
                Text = text,
                Position = position,
                StartTime = DateTime.Now,
                Duration = duration,
                Opacity = 1.0f
            };
        }

        public void HideTooltip()
        {
            _currentTooltip = null;
        }

        public void Draw(Graphics g)
        {
            if (!_currentTooltip.HasValue) return;

            var tooltip = _currentTooltip.Value;
            var elapsed = (DateTime.Now - tooltip.StartTime).TotalMilliseconds;

            if (elapsed > tooltip.Duration)
            {
                _currentTooltip = null;
                return;
            }

            // 淡出效果
            if (elapsed > tooltip.Duration - 500)
            {
                tooltip.Opacity = (float)((tooltip.Duration - elapsed) / 500.0);
            }

            var textSize = g.MeasureString(tooltip.Text, _font);
            var padding = 6;
            var rect = new Rectangle(
                tooltip.Position.X + 10,
                tooltip.Position.Y - (int)textSize.Height - 10,
                (int)textSize.Width + padding * 2,
                (int)textSize.Height + padding);

            using (var brush = new SolidBrush(Color.FromArgb((int)(240 * tooltip.Opacity), 255, 255, 255)))
            using (var textBrush = new SolidBrush(Color.FromArgb((int)(255 * tooltip.Opacity), 0, 0, 0)))
            using (var pen = new Pen(Color.FromArgb((int)(255 * tooltip.Opacity), Color.Gray)))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
                g.DrawString(tooltip.Text, _font, textBrush, rect.X + padding, rect.Y + padding / 2);
            }
        }

        public void Dispose()
        {
            _font?.Dispose();
            _backgroundBrush?.Dispose();
            _textBrush?.Dispose();
            _borderPen?.Dispose();
        }
    }

    /// <summary>
    /// 动画管理器
    /// </summary>
    public class AnimationManager : IDisposable
    {
        private readonly List<Animation> _animations;
        private CrosshairAnimation _crosshairAnimation;

        public AnimationManager()
        {
            _animations = new List<Animation>();
        }

        public void StartAnimation(AnimationType type, int duration)
        {
            var animation = new Animation(type, duration);
            _animations.Add(animation);
        }

        public void AnimateCrosshair(Point from, Point to)
        {
            _crosshairAnimation = new CrosshairAnimation(from, to, 300);
        }

        public bool Update()
        {
            bool needsRedraw = false;

            // 更新十字准星动画
            if (_crosshairAnimation != null)
            {
                _crosshairAnimation.Update();
                needsRedraw = true;

                if (_crosshairAnimation.IsComplete)
                {
                    _crosshairAnimation = null;
                }
            }

            // 更新其他动画
            for (int i = _animations.Count - 1; i >= 0; i--)
            {
                _animations[i].Update();
                needsRedraw = true;

                if (_animations[i].IsComplete)
                {
                    _animations.RemoveAt(i);
                }
            }

            return needsRedraw;
        }

        public void Draw(Graphics g, Rectangle bounds)
        {
            // 绘制十字准星动画
            _crosshairAnimation?.Draw(g);

            // 绘制其他动画
            foreach (var animation in _animations)
            {
                animation.Draw(g, bounds);
            }
        }

        public void StopAllAnimations()
        {
            _animations.Clear();
            _crosshairAnimation = null;
        }

        public void Dispose()
        {
            _animations.Clear();
        }
    }

    /// <summary>
    /// 动画类型
    /// </summary>
    public enum AnimationType
    {
        FadeIn,
        FadeOut,
        Pulse,
        Highlight,
        Ripple
    }

    /// <summary>
    /// 事件参数类
    /// </summary>
    public class ShortcutTriggeredEventArgs : EventArgs
    {
        public string ShortcutName { get; }

        public ShortcutTriggeredEventArgs(string shortcutName)
        {
            ShortcutName = shortcutName;
        }
    }

    public class MouseActivityEventArgs : EventArgs
    {
        public Point Position { get; }
        public double TimeSinceLastMove { get; }

        public MouseActivityEventArgs(Point position, double timeSinceLastMove)
        {
            Position = position;
            TimeSinceLastMove = timeSinceLastMove;
        }
    }

    public class CrosshairPositionChangedEventArgs : EventArgs
    {
        public Point Position { get; }

        public CrosshairPositionChangedEventArgs(Point position)
        {
            Position = position;
        }
    }

    #endregion

    #region 动画实现（简化版）

    internal class Animation
    {
        public AnimationType Type { get; }
        public int Duration { get; }
        public DateTime StartTime { get; }
        public bool IsComplete { get; private set; }

        public Animation(AnimationType type, int duration)
        {
            Type = type;
            Duration = duration;
            StartTime = DateTime.Now;
            IsComplete = false;
        }

        public void Update()
        {
            var elapsed = (DateTime.Now - StartTime).TotalMilliseconds;
            if (elapsed >= Duration)
            {
                IsComplete = true;
            }
        }

        public void Draw(Graphics g, Rectangle bounds)
        {
            // 简化的动画绘制逻辑
            var progress = Math.Min(1.0, (DateTime.Now - StartTime).TotalMilliseconds / Duration);

            switch (Type)
            {
                case AnimationType.Highlight:
                    DrawHighlight(g, bounds, progress);
                    break;
                case AnimationType.Ripple:
                    DrawRipple(g, bounds, progress);
                    break;
            }
        }

        private void DrawHighlight(Graphics g, Rectangle bounds, double progress)
        {
            var opacity = (float)(1.0 - progress);
            using (var brush = new SolidBrush(Color.FromArgb((int)(100 * opacity), Color.Yellow)))
            {
                g.FillRectangle(brush, bounds);
            }
        }

        private void DrawRipple(Graphics g, Rectangle bounds, double progress)
        {
            var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            var radius = (int)(Math.Min(bounds.Width, bounds.Height) * progress / 2);

            var opacity = (float)(1.0 - progress);
            using (var pen = new Pen(Color.FromArgb((int)(255 * opacity), Color.Blue), 2))
            {
                g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            }
        }
    }

    internal class CrosshairAnimation
    {
        private readonly Point _from;
        private readonly Point _to;
        private readonly int _duration;
        private readonly DateTime _startTime;
        private Point _currentPosition;

        public Point CurrentPosition => _currentPosition;
        public bool IsComplete { get; private set; }

        public CrosshairAnimation(Point from, Point to, int duration)
        {
            _from = from;
            _to = to;
            _duration = duration;
            _startTime = DateTime.Now;
            _currentPosition = from;
            IsComplete = false;
        }

        public void Update()
        {
            var elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
            if (elapsed >= _duration)
            {
                _currentPosition = _to;
                IsComplete = true;
                return;
            }

            var progress = elapsed / _duration;
            var easeProgress = EaseInOutQuad(progress);

            _currentPosition = new Point(
                (int)(_from.X + (_to.X - _from.X) * easeProgress),
                (int)(_from.Y + (_to.Y - _from.Y) * easeProgress));
        }

        public void Draw(Graphics g)
        {
            // 绘制移动轨迹或特效
            using (var pen = new Pen(Color.Orange, 2))
            {
                g.DrawLine(pen, _from, _currentPosition);
            }

            using (var brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush, _currentPosition.X - 3, _currentPosition.Y - 3, 6, 6);
            }
        }

        private double EaseInOutQuad(double t)
        {
            return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }
    }

    #endregion
}