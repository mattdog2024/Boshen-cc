using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 十字准星管理器
    /// 提供丰富的十字准星样式和自定义功能
    /// </summary>
    public class CrosshairManager : IDisposable
    {
        #region 私有字段

        private Point _position;
        private CrosshairStyle _style;
        private CrosshairTheme _theme;
        private bool _visible;
        private float _opacity;
        private int _thickness;
        private int _length;
        private bool _showLabels;
        private bool _showCoordinates;
        private bool _animate;
        private float _animationPhase;
        private readonly List<ICrosshairEffect> _effects;
        private readonly Timer _animationTimer;

        // 绘图资源
        private Pen _mainPen;
        private Pen _secondaryPen;
        private SolidBrush _labelBrush;
        private SolidBrush _backgroundBrush;
        private Font _labelFont;
        private Font _coordinateFont;

        #endregion

        #region 构造函数

        public CrosshairManager()
        {
            _position = Point.Empty;
            _style = CrosshairStyle.Simple;
            _theme = CrosshairTheme.Default;
            _visible = true;
            _opacity = 1.0f;
            _thickness = 1;
            _length = 20;
            _showLabels = false;
            _showCoordinates = true;
            _animate = false;
            _animationPhase = 0f;
            _effects = new List<ICrosshairEffect>();

            _animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;

            InitializeResources();
            InitializeDefaultEffects();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 十字准星位置
        /// </summary>
        public Point Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// 十字准星样式
        /// </summary>
        public CrosshairStyle Style
        {
            get => _style;
            set
            {
                if (_style != value)
                {
                    _style = value;
                    UpdateResources();
                }
            }
        }

        /// <summary>
        /// 十字准星主题
        /// </summary>
        public CrosshairTheme Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    UpdateResources();
                }
            }
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        /// <summary>
        /// 透明度 (0.0 - 1.0)
        /// </summary>
        public float Opacity
        {
            get => _opacity;
            set
            {
                _opacity = Math.Max(0, Math.Min(1, value));
                UpdateResources();
            }
        }

        /// <summary>
        /// 线条粗细
        /// </summary>
        public int Thickness
        {
            get => _thickness;
            set
            {
                if (value > 0 && _thickness != value)
                {
                    _thickness = value;
                    UpdateResources();
                }
            }
        }

        /// <summary>
        /// 十字线长度
        /// </summary>
        public int Length
        {
            get => _length;
            set => _length = Math.Max(5, value);
        }

        /// <summary>
        /// 是否显示标签
        /// </summary>
        public bool ShowLabels
        {
            get => _showLabels;
            set => _showLabels = value;
        }

        /// <summary>
        /// 是否显示坐标
        /// </summary>
        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set => _showCoordinates = value;
        }

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool Animate
        {
            get => _animate;
            set
            {
                if (_animate != value)
                {
                    _animate = value;
                    if (value)
                        _animationTimer.Start();
                    else
                        _animationTimer.Stop();
                }
            }
        }

        /// <summary>
        /// 动画相位
        /// </summary>
        public float AnimationPhase => _animationPhase;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置十字准星样式和主题
        /// </summary>
        /// <param name="style">样式</param>
        /// <param name="theme">主题</param>
        public void SetStyle(CrosshairStyle style, CrosshairTheme theme = CrosshairTheme.Default)
        {
            Style = style;
            Theme = theme;
        }

        /// <summary>
        /// 添加十字准星效果
        /// </summary>
        /// <param name="effect">效果</param>
        public void AddEffect(ICrosshairEffect effect)
        {
            if (effect != null && !_effects.Contains(effect))
            {
                _effects.Add(effect);
            }
        }

        /// <summary>
        /// 移除十字准星效果
        /// </summary>
        /// <param name="effect">效果</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveEffect(ICrosshairEffect effect)
        {
            return _effects.Remove(effect);
        }

        /// <summary>
        /// 清除所有效果
        /// </summary>
        public void ClearEffects()
        {
            _effects.Clear();
        }

        /// <summary>
        /// 移动十字准星到指定位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        public void MoveTo(int x, int y)
        {
            _position = new Point(x, y);
        }

        /// <summary>
        /// 移动十字准星到指定位置
        /// </summary>
        /// <param name="position">位置</param>
        public void MoveTo(Point position)
        {
            _position = position;
        }

        /// <summary>
        /// 显示十字准星
        /// </summary>
        public void Show()
        {
            Visible = true;
        }

        /// <summary>
        /// 隐藏十字准星
        /// </summary>
        public void Hide()
        {
            Visible = false;
        }

        /// <summary>
        /// 切换可见性
        /// </summary>
        public void Toggle()
        {
            Visible = !Visible;
        }

        /// <summary>
        /// 淡入效果
        /// </summary>
        /// <param name="duration">持续时间（毫秒）</param>
        public void FadeIn(int duration = 500)
        {
            AddEffect(new FadeEffect(0f, 1f, duration));
        }

        /// <summary>
        /// 淡出效果
        /// </summary>
        /// <param name="duration">持续时间（毫秒）</param>
        public void FadeOut(int duration = 500)
        {
            AddEffect(new FadeEffect(1f, 0f, duration));
        }

        /// <summary>
        /// 脉冲效果
        /// </summary>
        /// <param name="duration">持续时间（毫秒）</param>
        public void Pulse(int duration = 1000)
        {
            AddEffect(new PulseEffect(duration));
        }

        /// <summary>
        /// 绘制十字准星
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制边界</param>
        /// <param name="position">位置（可选，默认使用当前位置）</param>
        public void Draw(Graphics graphics, Rectangle bounds, Point? position = null)
        {
            if (!_visible || graphics == null) return;

            var drawPosition = position ?? _position;
            if (!bounds.Contains(drawPosition)) return;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 应用效果
            var effectOpacity = ApplyEffects();

            // 更新资源透明度
            UpdateResourceOpacity(effectOpacity);

            // 根据样式绘制
            switch (_style)
            {
                case CrosshairStyle.Simple:
                    DrawSimpleCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Full:
                    DrawFullCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Dashed:
                    DrawDashedCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Circle:
                    DrawCircleCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Cross:
                    DrawCrossCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Box:
                    DrawBoxCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Diagonal:
                    DrawDiagonalCrosshair(graphics, bounds, drawPosition);
                    break;
                case CrosshairStyle.Custom:
                    DrawCustomCrosshair(graphics, bounds, drawPosition);
                    break;
            }

            // 绘制标签和坐标
            if (_showLabels || _showCoordinates)
            {
                DrawLabels(graphics, bounds, drawPosition);
            }
        }

        /// <summary>
        /// 获取十字准星的边界矩形
        /// </summary>
        /// <returns>边界矩形</returns>
        public Rectangle GetBounds()
        {
            var size = _length * 2 + _thickness;
            return new Rectangle(
                _position.X - size / 2,
                _position.Y - size / 2,
                size,
                size);
        }

        /// <summary>
        /// 检查点是否在十字准星范围内
        /// </summary>
        /// <param name="point">检查点</param>
        /// <returns>是否在范围内</returns>
        public bool ContainsPoint(Point point)
        {
            var bounds = GetBounds();
            return bounds.Contains(point);
        }

        #endregion

        #region 私有方法

        private void InitializeResources()
        {
            _labelFont = new Font("Arial", 8, FontStyle.Bold);
            _coordinateFont = new Font("Arial", 7);
            UpdateResources();
        }

        private void UpdateResources()
        {
            // 清理旧资源
            _mainPen?.Dispose();
            _secondaryPen?.Dispose();
            _labelBrush?.Dispose();
            _backgroundBrush?.Dispose();

            // 获取主题颜色
            var mainColor = GetThemeColor(_theme, ThemeColorType.Main);
            var secondaryColor = GetThemeColor(_theme, ThemeColorType.Secondary);
            var labelColor = GetThemeColor(_theme, ThemeColorType.Label);
            var backgroundColor = GetThemeColor(_theme, ThemeColorType.Background);

            // 创建新资源
            var alpha = (int)(255 * _opacity);
            _mainPen = new Pen(Color.FromArgb(alpha, mainColor), _thickness);
            _secondaryPen = new Pen(Color.FromArgb(alpha, secondaryColor), _thickness);
            _labelBrush = new SolidBrush(Color.FromArgb(alpha, labelColor));
            _backgroundBrush = new SolidBrush(Color.FromArgb(alpha, backgroundColor));

            // 应用样式特定的设置
            ApplyStyleSettings();
        }

        private void UpdateResourceOpacity(float effectOpacity)
        {
            var alpha = (int)(255 * _opacity * effectOpacity);
            var mainColor = GetThemeColor(_theme, ThemeColorType.Main);
            var secondaryColor = GetThemeColor(_theme, ThemeColorType.Secondary);
            var labelColor = GetThemeColor(_theme, ThemeColorType.Label);
            var backgroundColor = GetThemeColor(_theme, ThemeColorType.Background);

            _mainPen.Color = Color.FromArgb(alpha, mainColor);
            _secondaryPen.Color = Color.FromArgb(alpha, secondaryColor);
            _labelBrush.Color = Color.FromArgb(alpha, labelColor);
            _backgroundBrush.Color = Color.FromArgb(alpha, backgroundColor);
        }

        private void InitializeDefaultEffects()
        {
            // 可以添加默认效果
        }

        private void ApplyStyleSettings()
        {
            switch (_style)
            {
                case CrosshairStyle.Dashed:
                    _mainPen.DashStyle = DashStyle.Dash;
                    break;
                case CrosshairStyle.Simple:
                case CrosshairStyle.Full:
                case CrosshairStyle.Circle:
                case CrosshairStyle.Cross:
                case CrosshairStyle.Box:
                case CrosshairStyle.Diagonal:
                case CrosshairStyle.Custom:
                default:
                    _mainPen.DashStyle = DashStyle.Solid;
                    break;
            }
        }

        private Color GetThemeColor(CrosshairTheme theme, ThemeColorType colorType)
        {
            return theme switch
            {
                CrosshairTheme.Dark => colorType switch
                {
                    ThemeColorType.Main => Color.White,
                    ThemeColorType.Secondary => Color.Gray,
                    ThemeColorType.Label => Color.White,
                    ThemeColorType.Background => Color.Black,
                    _ => Color.White
                },
                CrosshairTheme.Light => colorType switch
                {
                    ThemeColorType.Main => Color.Black,
                    ThemeColorType.Secondary => Color.DarkGray,
                    ThemeColorType.Label => Color.Black,
                    ThemeColorType.Background => Color.White,
                    _ => Color.Black
                },
                CrosshairTheme.Blue => colorType switch
                {
                    ThemeColorType.Main => Color.Blue,
                    ThemeColorType.Secondary => Color.LightBlue,
                    ThemeColorType.Label => Color.DarkBlue,
                    ThemeColorType.Background => Color.FromArgb(240, 240, 255),
                    _ => Color.Blue
                },
                CrosshairTheme.Green => colorType switch
                {
                    ThemeColorType.Main => Color.Green,
                    ThemeColorType.Secondary => Color.LightGreen,
                    ThemeColorType.Label => Color.DarkGreen,
                    ThemeColorType.Background => Color.FromArgb(240, 255, 240),
                    _ => Color.Green
                },
                CrosshairTheme.Red => colorType switch
                {
                    ThemeColorType.Main => Color.Red,
                    ThemeColorType.Secondary => Color.LightCoral,
                    ThemeColorType.Label => Color.DarkRed,
                    ThemeColorType.Background => Color.FromArgb(255, 240, 240),
                    _ => Color.Red
                },
                CrosshairTheme.Default:
                default:
                    return colorType switch
                    {
                        ThemeColorType.Main => Color.Gray,
                        ThemeColorType.Secondary => Color.LightGray,
                        ThemeColorType.Label => Color.Black,
                        ThemeColorType.Background => Color.White,
                        _ => Color.Gray
                    };
            };
        }

        private float ApplyEffects()
        {
            var totalOpacity = 1.0f;

            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.Update(_animationPhase);

                if (effect.IsComplete)
                {
                    _effects.RemoveAt(i);
                }
                else if (effect is FadeEffect fadeEffect)
                {
                    totalOpacity *= fadeEffect.CurrentOpacity;
                }
            }

            return totalOpacity;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _animationPhase += 0.016f; // ~60 FPS
            if (_animationPhase > 1f)
                _animationPhase -= 1f;
        }

        #region 具体绘制方法

        private void DrawSimpleCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            // 水平线
            g.DrawLine(_mainPen, position.X - _length, position.Y, position.X + _length, position.Y);
            // 垂直线
            g.DrawLine(_mainPen, position.X, position.Y - _length, position.X, position.Y + _length);
        }

        private void DrawFullCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            // 水平线贯穿整个控件
            g.DrawLine(_mainPen, bounds.Left, position.Y, bounds.Right, position.Y);
            // 垂直线贯穿整个控件
            g.DrawLine(_mainPen, position.X, bounds.Top, position.X, bounds.Bottom);
        }

        private void DrawDashedCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            var originalDashStyle = _mainPen.DashStyle;
            _mainPen.DashStyle = DashStyle.Dash;
            DrawSimpleCrosshair(g, bounds, position);
            _mainPen.DashStyle = originalDashStyle;
        }

        private void DrawCircleCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            var radius = _length;
            var rect = new Rectangle(position.X - radius, position.Y - radius, radius * 2, radius * 2);

            g.DrawEllipse(_mainPen, rect);

            // 中心十字
            g.DrawLine(_mainPen, position.X - 5, position.Y, position.X + 5, position.Y);
            g.DrawLine(_mainPen, position.X, position.Y - 5, position.X, position.Y + 5);
        }

        private void DrawCrossCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            var size = _length;

            // 绘制X形十字
            g.DrawLine(_mainPen, position.X - size, position.Y - size, position.X + size, position.Y + size);
            g.DrawLine(_mainPen, position.X - size, position.Y + size, position.X + size, position.Y - size);

            // 中心点
            g.FillEllipse(_labelBrush, position.X - 2, position.Y - 2, 4, 4);
        }

        private void DrawBoxCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            var size = _length / 2;
            var rect = new Rectangle(position.X - size, position.Y - size, size * 2, size * 2);

            g.DrawRectangle(_mainPen, rect);

            // 中心十字
            g.DrawLine(_secondaryPen, rect.Left, position.Y, rect.Right, position.Y);
            g.DrawLine(_secondaryPen, position.X, rect.Top, position.X, rect.Bottom);
        }

        private void DrawDiagonalCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            var size = _length;

            // 对角线
            g.DrawLine(_mainPen, position.X - size, position.Y - size, position.X + size, position.Y + size);
            g.DrawLine(_mainPen, position.X - size, position.Y + size, position.X + size, position.Y - size);

            // 水平和垂直辅助线
            g.DrawLine(_secondaryPen, position.X - size / 2, position.Y, position.X + size / 2, position.Y);
            g.DrawLine(_secondaryPen, position.X, position.Y - size / 2, position.X, position.Y + size / 2);
        }

        private void DrawCustomCrosshair(Graphics g, Rectangle bounds, Point position)
        {
            // 自定义样式：组合多种元素
            DrawSimpleCrosshair(g, bounds, position);

            // 添加圆形装饰
            var radius = _length + 5;
            g.DrawEllipse(_secondaryPen, position.X - radius, position.Y - radius, radius * 2, radius * 2);
        }

        private void DrawLabels(Graphics g, Rectangle bounds, Point position)
        {
            var labelY = position.Y - 20;

            if (_showCoordinates)
            {
                var coordText = $"X:{position.X} Y:{position.Y}";
                var coordSize = g.MeasureString(coordText, _coordinateFont);
                var coordRect = new Rectangle(
                    position.X + 10,
                    position.Y - (int)coordSize.Height - 5,
                    (int)coordSize.Width + 6,
                    (int)coordSize.Height + 2);

                // 确保标签在边界内
                if (coordRect.Right > bounds.Right)
                    coordRect.X = position.X - (int)coordSize.Width - 16;
                if (coordRect.Top < bounds.Top)
                    coordRect.Y = position.Y + 10;

                // 绘制背景
                g.FillRectangle(_backgroundBrush, coordRect);
                g.DrawRectangle(_mainPen, coordRect);

                // 绘制文本
                g.DrawString(coordText, _coordinateFont, _labelBrush, coordRect.X + 3, coordRect.Y + 1);
            }

            if (_showLabels)
            {
                // 绘制方向标签
                var labels = new[] { "N", "E", "S", "W" };
                var positions = new[]
                {
                    new Point(position.X, position.Y - _length - 10),
                    new Point(position.X + _length + 10, position.Y),
                    new Point(position.X, position.Y + _length + 10),
                    new Point(position.X - _length - 20, position.Y)
                };

                for (int i = 0; i < labels.Length; i++)
                {
                    var labelSize = g.MeasureString(labels[i], _labelFont);
                    var labelRect = new Rectangle(
                        positions[i].X - (int)labelSize.Width / 2,
                        positions[i].Y - (int)labelSize.Height / 2,
                        (int)labelSize.Width,
                        (int)labelSize.Height);

                    // 确保标签在边界内
                    if (labelRect.Left < bounds.Left) labelRect.X = bounds.Left;
                    if (labelRect.Right > bounds.Right) labelRect.X = bounds.Right - labelRect.Width;
                    if (labelRect.Top < bounds.Top) labelRect.Y = bounds.Top;
                    if (labelRect.Bottom > bounds.Bottom) labelRect.Y = bounds.Bottom - labelRect.Height;

                    g.DrawString(labels[i], _labelFont, _labelBrush, labelRect);
                }
            }
        }

        #endregion

        #endregion

        #region 资源释放

        public void Dispose()
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
            _mainPen?.Dispose();
            _secondaryPen?.Dispose();
            _labelBrush?.Dispose();
            _backgroundBrush?.Dispose();
            _labelFont?.Dispose();
            _coordinateFont?.Dispose();
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 十字准星样式
    /// </summary>
    public enum CrosshairStyle
    {
        Simple,    // 简单十字线
        Full,      // 全屏十字线
        Dashed,    // 虚线十字线
        Circle,    // 圆形十字线
        Cross,     // X形十字线
        Box,       // 矩形十字线
        Diagonal,  // 对角十字线
        Custom     // 自定义样式
    }

    /// <summary>
    /// 十字准星主题
    /// </summary>
    public enum CrosshairTheme
    {
        Default,   // 默认主题
        Dark,      // 深色主题
        Light,     // 浅色主题
        Blue,      // 蓝色主题
        Green,     // 绿色主题
        Red        // 红色主题
    }

    /// <summary>
    /// 主题颜色类型
    /// </summary>
    private enum ThemeColorType
    {
        Main,       // 主要颜色
        Secondary,  // 次要颜色
        Label,      // 标签颜色
        Background  // 背景颜色
    }

    #endregion

    #region 效果接口和实现

    /// <summary>
    /// 十字准星效果接口
    /// </summary>
    public interface ICrosshairEffect
    {
        bool IsComplete { get; }
        void Update(float animationPhase);
    }

    /// <summary>
    /// 淡入淡出效果
    /// </summary>
    public class FadeEffect : ICrosshairEffect
    {
        private readonly float _fromOpacity;
        private readonly float _toOpacity;
        private readonly int _duration;
        private readonly DateTime _startTime;
        private float _currentOpacity;

        public bool IsComplete { get; private set; }
        public float CurrentOpacity => _currentOpacity;

        public FadeEffect(float fromOpacity, float toOpacity, int duration)
        {
            _fromOpacity = fromOpacity;
            _toOpacity = toOpacity;
            _duration = duration;
            _startTime = DateTime.Now;
            _currentOpacity = fromOpacity;
            IsComplete = false;
        }

        public void Update(float animationPhase)
        {
            var elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
            var progress = Math.Min(1.0, elapsed / _duration);

            _currentOpacity = _fromOpacity + (_toOpacity - _fromOpacity) * progress;

            if (progress >= 1.0)
            {
                IsComplete = true;
            }
        }
    }

    /// <summary>
    /// 脉冲效果
    /// </summary>
    public class PulseEffect : ICrosshairEffect
    {
        private readonly int _duration;
        private readonly DateTime _startTime;

        public bool IsComplete { get; private set; }

        public PulseEffect(int duration)
        {
            _duration = duration;
            _startTime = DateTime.Now;
            IsComplete = false;
        }

        public void Update(float animationPhase)
        {
            var elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
            if (elapsed >= _duration)
            {
                IsComplete = true;
            }
        }
    }

    #endregion
}