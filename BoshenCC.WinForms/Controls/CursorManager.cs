using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoshenCC.Core;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 鼠标光标管理器
    /// 提供自定义光标样式、状态管理和动画效果
    /// </summary>
    public class CursorManager : IDisposable
    {
        #region Win32 API导入

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        #endregion

        #region 私有字段

        private readonly Control _targetControl;
        private readonly Dictionary<CursorType, Cursor> _customCursors;
        private readonly Dictionary<CursorType, CursorAnimation> _cursorAnimations;
        private CursorType _currentCursorType;
        private CursorType _previousCursorType;
        private Cursor _originalCursor;
        private readonly Timer _animationTimer;
        private CursorAnimation _currentAnimation;
        private int _animationFrame;
        private bool _disposed;
        private readonly object _lockObject = new object();

        // 十字准星绘制相关
        private bool _showCrosshair;
        private Point _crosshairPosition;
        private CrosshairStyle _crosshairStyle;
        private Color _crosshairColor;
        private int _crosshairSize;
        private float _crosshairOpacity;
        private Bitmap _crosshairBuffer;
        private Graphics _crosshairGraphics;

        #endregion

        #region 事件

        /// <summary>
        /// 光标改变事件
        /// </summary>
        public event EventHandler<CursorChangedEventArgs> CursorChanged;

        /// <summary>
        /// 十字准星显示状态改变事件
        /// </summary>
        public event EventHandler<CrosshairStateChangedEventArgs> CrosshairStateChanged;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化CursorManager类
        /// </summary>
        /// <param name="targetControl">目标控件</param>
        public CursorManager(Control targetControl)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));

            _customCursors = new Dictionary<CursorType, Cursor>();
            _cursorAnimations = new Dictionary<CursorType, CursorAnimation>();
            _currentCursorType = CursorType.Default;
            _previousCursorType = CursorType.Default;
            _originalCursor = _targetControl.Cursor;

            InitializeCrosshairSettings();
            InitializeAnimationTimer();
            CreateCustomCursors();
            AttachEvents();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前光标类型
        /// </summary>
        public CursorType CurrentCursorType
        {
            get => _currentCursorType;
            private set
            {
                if (_currentCursorType != value)
                {
                    _previousCursorType = _currentCursorType;
                    _currentCursorType = value;
                    UpdateCursor();
                    OnCursorChanged(new CursorChangedEventArgs(_previousCursorType, _currentCursorType));
                }
            }
        }

        /// <summary>
        /// 前一个光标类型
        /// </summary>
        public CursorType PreviousCursorType => _previousCursorType;

        /// <summary>
        /// 是否显示十字准星
        /// </summary>
        public bool ShowCrosshair
        {
            get => _showCrosshair;
            set
            {
                if (_showCrosshair != value)
                {
                    _showCrosshair = value;
                    if (_showCrosshair)
                    {
                        InitializeCrosshairBuffer();
                    }
                    else
                    {
                        CleanupCrosshairBuffer();
                    }
                    _targetControl.Invalidate();
                    OnCrosshairStateChanged(new CrosshairStateChangedEventArgs(_showCrosshair, _crosshairPosition));
                }
            }
        }

        /// <summary>
        /// 十字准星位置
        /// </summary>
        public Point CrosshairPosition
        {
            get => _crosshairPosition;
            set
            {
                if (_crosshairPosition != value)
                {
                    _crosshairPosition = value;
                    if (_showCrosshair)
                    {
                        _targetControl.Invalidate();
                    }
                    OnCrosshairStateChanged(new CrosshairStateChangedEventArgs(_showCrosshair, _crosshairPosition));
                }
            }
        }

        /// <summary>
        /// 十字准星样式
        /// </summary>
        public CrosshairStyle CrosshairStyle
        {
            get => _crosshairStyle;
            set
            {
                if (_crosshairStyle != value)
                {
                    _crosshairStyle = value;
                    if (_showCrosshair)
                    {
                        _targetControl.Invalidate();
                    }
                }
            }
        }

        /// <summary>
        /// 十字准星颜色
        /// </summary>
        public Color CrosshairColor
        {
            get => _crosshairColor;
            set
            {
                if (_crosshairColor != value)
                {
                    _crosshairColor = value;
                    if (_showCrosshair)
                    {
                        _targetControl.Invalidate();
                    }
                }
            }
        }

        /// <summary>
        /// 十字准星大小
        /// </summary>
        public int CrosshairSize
        {
            get => _crosshairSize;
            set
            {
                if (_crosshairSize != value)
                {
                    _crosshairSize = Math.Max(10, Math.Min(200, value));
                    if (_showCrosshair)
                    {
                        _targetControl.Invalidate();
                    }
                }
            }
        }

        /// <summary>
        /// 十字准星透明度
        /// </summary>
        public float CrosshairOpacity
        {
            get => _crosshairOpacity;
            set
            {
                if (Math.Abs(_crosshairOpacity - value) > 0.01f)
                {
                    _crosshairOpacity = Math.Max(0f, Math.Min(1f, value));
                    if (_showCrosshair)
                    {
                        _targetControl.Invalidate();
                    }
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置光标类型
        /// </summary>
        /// <param name="cursorType">光标类型</param>
        public void SetCursor(CursorType cursorType)
        {
            CurrentCursorType = cursorType;
        }

        /// <summary>
        /// 设置临时光标（一段时间后恢复）
        /// </summary>
        /// <param name="cursorType">临时光标类型</param>
        /// <param name="duration">持续时间（毫秒）</param>
        public void SetTemporaryCursor(CursorType cursorType, int duration)
        {
            var originalType = CurrentCursorType;
            SetCursor(cursorType);

            var timer = new Timer { Interval = duration };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                SetCursor(originalType);
            };
            timer.Start();
        }

        /// <summary>
        /// 恢复到默认光标
        /// </summary>
        public void RestoreDefaultCursor()
        {
            CurrentCursorType = CursorType.Default;
        }

        /// <summary>
        /// 显示十字准星
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="style">样式</param>
        public void ShowCrosshairAt(Point position, CrosshairStyle style = CrosshairStyle.Simple)
        {
            CrosshairPosition = position;
            CrosshairStyle = style;
            ShowCrosshair = true;
        }

        /// <summary>
        /// 隐藏十字准星
        /// </summary>
        public void HideCrosshair()
        {
            ShowCrosshair = false;
        }

        /// <summary>
        /// 绘制十字准星
        /// </summary>
        /// <param name="g">绘图对象</param>
        /// <param name="offset">偏移量</param>
        public void DrawCrosshair(Graphics g, Point offset)
        {
            if (!_showCrosshair || g == null)
                return;

            // 保存原始状态
            var originalSmoothingMode = g.SmoothingMode;
            var originalCompositeMode = g.CompositingMode;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingMode = CompositingMode.SourceOver;

            using (var crosshairColor = Color.FromArgb((int)(_crosshairOpacity * 255), _crosshairColor))
            using (var pen = new Pen(crosshairColor, 1f))
            {
                var center = new Point(_crosshairPosition.X - offset.X, _crosshairPosition.Y - offset.Y);

                switch (_crosshairStyle)
                {
                    case CrosshairStyle.Simple:
                        DrawSimpleCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Full:
                        DrawFullCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Dashed:
                        DrawDashedCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Circle:
                        DrawCircleCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Plus:
                        DrawPlusCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Rectangle:
                        DrawRectangleCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Diagonal:
                        DrawDiagonalCrosshair(g, pen, center);
                        break;
                    case CrosshairStyle.Custom:
                        DrawCustomCrosshair(g, pen, center);
                        break;
                }
            }

            // 恢复原始状态
            g.SmoothingMode = originalSmoothingMode;
            g.CompositingMode = originalCompositeMode;
        }

        /// <summary>
        /// 启动光标动画
        /// </summary>
        /// <param name="cursorType">光标类型</param>
        public void StartCursorAnimation(CursorType cursorType)
        {
            if (_cursorAnimations.ContainsKey(cursorType))
            {
                _currentAnimation = _cursorAnimations[cursorType];
                _animationFrame = 0;
                _animationTimer.Start();
            }
        }

        /// <summary>
        /// 停止光标动画
        /// </summary>
        public void StopCursorAnimation()
        {
            _animationTimer.Stop();
            _currentAnimation = null;
            _animationFrame = 0;
        }

        /// <summary>
        /// 创建自定义光标
        /// </summary>
        /// <param name="cursorType">光标类型</param>
        /// <param name="bitmap">光标图像</param>
        /// <param name="hotSpot">热点位置</param>
        public void CreateCustomCursor(CursorType cursorType, Bitmap bitmap, Point hotSpot)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var cursor = CreateCursor(bitmap, hotSpot);
            _customCursors[cursorType] = cursor;
        }

        /// <summary>
        /// 获取光标描述
        /// </summary>
        /// <param name="cursorType">光标类型</param>
        /// <returns>描述文本</returns>
        public string GetCursorDescription(CursorType cursorType)
        {
            switch (cursorType)
            {
                case CursorType.Default: return "默认光标";
                case CursorType.Crosshair: return "十字准星";
                case CursorType.Hand: return "手型光标";
                case CursorType.Help: return "帮助光标";
                case CursorType.Wait: return "等待光标";
                case CursorType.No: return "禁止光标";
                case CursorType.SizeAll: return "调整大小";
                case CursorType.SizeNESW: return "东北西南调整";
                case CursorType.SizeNS: return "南北调整";
                case CursorType.SizeNWSE: return "西北东南调整";
                case CursorType.SizeWE: return "东西调整";
                case CursorType.UpArrow: return "向上箭头";
                case CursorType.IBeam: return "文本光标";
                case CursorType.Measuring: return "测量光标";
                case CursorType.Drawing: return "绘制光标";
                case CursorType.Selecting: return "选择光标";
                case CursorType.Grabbing: return "抓取光标";
                case CursorType.ZoomIn: return "放大光标";
                case CursorType.ZoomOut: return "缩小光标";
                default: return "未知光标";
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化十字准星设置
        /// </summary>
        private void InitializeCrosshairSettings()
        {
            _showCrosshair = false;
            _crosshairPosition = Point.Empty;
            _crosshairStyle = CrosshairStyle.Simple;
            _crosshairColor = Color.Red;
            _crosshairSize = 50;
            _crosshairOpacity = 0.8f;
        }

        /// <summary>
        /// 初始化动画定时器
        /// </summary>
        private void InitializeAnimationTimer()
        {
            _animationTimer = new Timer { Interval = 100 }; // 10 FPS
            _animationTimer.Tick += OnAnimationTimerTick;
        }

        /// <summary>
        /// 创建自定义光标
        /// </summary>
        private void CreateCustomCursors()
        {
            // 创建测量光标
            CreateMeasuringCursor();

            // 创建绘制光标
            CreateDrawingCursor();

            // 创建选择光标
            CreateSelectingCursor();

            // 创建放大缩小光标
            CreateZoomCursors();
        }

        /// <summary>
        /// 创建测量光标
        /// </summary>
        private void CreateMeasuringCursor()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                // 绘制尺子形状的光标
                using (var pen = new Pen(Color.Black, 2))
                {
                    // 横线
                    g.DrawLine(pen, 2, 16, 30, 16);
                    // 左侧竖线
                    g.DrawLine(pen, 2, 8, 2, 24);
                    // 右侧竖线
                    g.DrawLine(pen, 30, 8, 30, 24);
                    // 刻度线
                    for (int i = 1; i < 7; i++)
                    {
                        var x = 2 + i * 4;
                        g.DrawLine(pen, x, 14, x, 18);
                    }
                }
            }

            _customCursors[CursorType.Measuring] = CreateCursor(bitmap, new Point(2, 16));
            bitmap.Dispose();
        }

        /// <summary>
        /// 创建绘制光标
        /// </summary>
        private void CreateDrawingCursor()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                using (var pen = new Pen(Color.Black, 2))
                {
                    // 绘制铅笔形状
                    g.DrawLine(pen, 2, 30, 16, 2);
                    g.DrawLine(pen, 16, 2, 20, 6);
                    g.DrawLine(pen, 20, 6, 18, 8);
                    g.DrawLine(pen, 18, 8, 14, 4);
                    g.DrawLine(pen, 14, 4, 2, 30);
                }
            }

            _customCursors[CursorType.Drawing] = CreateCursor(bitmap, new Point(2, 30));
            bitmap.Dispose();
        }

        /// <summary>
        /// 创建选择光标
        /// </summary>
        private void CreateSelectingCursor()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                // 绘制选择框形状
                using (var pen = new Pen(Color.Black, 2))
                {
                    g.DrawRectangle(pen, 2, 2, 20, 20);
                    // 虚线效果
                    for (int i = 6; i < 20; i += 4)
                    {
                        g.DrawLine(pen, 2, i, 4, i);
                        g.DrawLine(pen, 20, i, 22, i);
                        g.DrawLine(pen, i, 2, i, 4);
                        g.DrawLine(pen, i, 20, i, 22);
                    }
                }
            }

            _customCursors[CursorType.Selecting] = CreateCursor(bitmap, new Point(2, 2));
            bitmap.Dispose();
        }

        /// <summary>
        /// 创建放大缩小光标
        /// </summary>
        private void CreateZoomCursors()
        {
            // 放大光标
            CreateZoomCursor(CursorType.ZoomIn, '+');

            // 缩小光标
            CreateZoomCursor(CursorType.ZoomOut, '-');
        }

        /// <summary>
        /// 创建缩放光标
        /// </summary>
        private void CreateZoomCursor(CursorType cursorType, char symbol)
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                using (var pen = new Pen(Color.Black, 2))
                using (var brush = new SolidBrush(Color.White))
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                {
                    // 绘制圆形背景
                    g.FillEllipse(brush, 4, 4, 24, 24);
                    g.DrawEllipse(pen, 4, 4, 24, 24);

                    // 绘制符号
                    var textSize = g.MeasureString(symbol.ToString(), font);
                    var textX = 16 - textSize.Width / 2;
                    var textY = 16 - textSize.Height / 2;
                    g.DrawString(symbol.ToString(), font, Brushes.Black, textX, textY);
                }
            }

            _customCursors[cursorType] = CreateCursor(bitmap, new Point(16, 16));
            bitmap.Dispose();
        }

        /// <summary>
        /// 从位图创建光标
        /// </summary>
        private Cursor CreateCursor(Bitmap bitmap, Point hotSpot)
        {
            var iconInfo = new IconInfo();
            GetIconInfo(bitmap.GetHicon(), ref iconInfo);
            iconInfo.xHotspot = hotSpot.X;
            iconInfo.yHotspot = hotSpot.Y;
            iconInfo.fIcon = false;
            var cursorPtr = CreateIconIndirect(ref iconInfo);
            return new Cursor(cursorPtr);
        }

        /// <summary>
        /// 更新光标
        /// </summary>
        private void UpdateCursor()
        {
            if (_customCursors.ContainsKey(_currentCursorType))
            {
                _targetControl.Cursor = _customCursors[_currentCursorType];
            }
            else
            {
                // 使用系统默认光标
                switch (_currentCursorType)
                {
                    case CursorType.Default:
                        _targetControl.Cursor = Cursors.Default;
                        break;
                    case CursorType.Crosshair:
                        _targetControl.Cursor = Cursors.Cross;
                        break;
                    case CursorType.Hand:
                        _targetControl.Cursor = Cursors.Hand;
                        break;
                    case CursorType.Help:
                        _targetControl.Cursor = Cursors.Help;
                        break;
                    case CursorType.Wait:
                        _targetControl.Cursor = Cursors.WaitCursor;
                        break;
                    case CursorType.No:
                        _targetControl.Cursor = Cursors.No;
                        break;
                    case CursorType.SizeAll:
                        _targetControl.Cursor = Cursors.SizeAll;
                        break;
                    case CursorType.SizeNESW:
                        _targetControl.Cursor = Cursors.SizeNESW;
                        break;
                    case CursorType.SizeNS:
                        _targetControl.Cursor = Cursors.SizeNS;
                        break;
                    case CursorType.SizeNWSE:
                        _targetControl.Cursor = Cursors.SizeNWSE;
                        break;
                    case CursorType.SizeWE:
                        _targetControl.Cursor = Cursors.SizeWE;
                        break;
                    case CursorType.UpArrow:
                        _targetControl.Cursor = Cursors.UpArrow;
                        break;
                    case CursorType.IBeam:
                        _targetControl.Cursor = Cursors.IBeam;
                        break;
                    default:
                        _targetControl.Cursor = Cursors.Default;
                        break;
                }
            }
        }

        /// <summary>
        /// 初始化十字准星缓冲区
        /// </summary>
        private void InitializeCrosshairBuffer()
        {
            if (_crosshairBuffer == null)
            {
                _crosshairBuffer = new Bitmap(_targetControl.Width, _targetControl.Height);
                _crosshairGraphics = Graphics.FromImage(_crosshairBuffer);
            }
        }

        /// <summary>
        /// 清理十字准星缓冲区
        /// </summary>
        private void CleanupCrosshairBuffer()
        {
            _crosshairGraphics?.Dispose();
            _crosshairBuffer?.Dispose();
            _crosshairGraphics = null;
            _crosshairBuffer = null;
        }

        /// <summary>
        /// 附加事件处理程序
        /// </summary>
        private void AttachEvents()
        {
            _targetControl.MouseMove += OnTargetControlMouseMove;
            _targetControl.Resize += OnTargetControlResize;
        }

        /// <summary>
        /// 分离事件处理程序
        /// </summary>
        private void DetachEvents()
        {
            _targetControl.MouseMove -= OnTargetControlMouseMove;
            _targetControl.Resize -= OnTargetControlResize;
        }

        #endregion

        #region 十字准星绘制方法

        private void DrawSimpleCrosshair(Graphics g, Pen pen, Point center)
        {
            var halfSize = _crosshairSize / 2;
            g.DrawLine(pen, center.X - halfSize, center.Y, center.X + halfSize, center.Y);
            g.DrawLine(pen, center.X, center.Y - halfSize, center.X, center.Y + halfSize);
        }

        private void DrawFullCrosshair(Graphics g, Pen pen, Point center)
        {
            if (_targetControl != null)
            {
                // 全屏十字准星
                g.DrawLine(pen, 0, center.Y, _targetControl.Width, center.Y);
                g.DrawLine(pen, center.X, 0, center.X, _targetControl.Height);
            }
        }

        private void DrawDashedCrosshair(Graphics g, Pen pen, Point center)
        {
            using (var dashedPen = new Pen(pen.Color, pen.Width)
            {
                DashPattern = new float[] { 5, 5 }
            })
            {
                DrawSimpleCrosshair(g, dashedPen, center);
            }
        }

        private void DrawCircleCrosshair(Graphics g, Pen pen, Point center)
        {
            var radius = _crosshairSize / 2;
            g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            // 添加十字线
            DrawSimpleCrosshair(g, pen, center);
        }

        private void DrawPlusCrosshair(Graphics g, Pen pen, Point center)
        {
            var size = _crosshairSize / 3;
            g.DrawLine(pen, center.X - size, center.Y, center.X + size, center.Y);
            g.DrawLine(pen, center.X, center.Y - size, center.X, center.Y + size);

            // 在末端添加小横线
            g.DrawLine(pen, center.X - size, center.Y - 2, center.X - size, center.Y + 2);
            g.DrawLine(pen, center.X + size, center.Y - 2, center.X + size, center.Y + 2);
            g.DrawLine(pen, center.X - 2, center.Y - size, center.X + 2, center.Y - size);
            g.DrawLine(pen, center.X - 2, center.Y + size, center.X + 2, center.Y + size);
        }

        private void DrawRectangleCrosshair(Graphics g, Pen pen, Point center)
        {
            var halfSize = _crosshairSize / 2;
            g.DrawRectangle(pen, center.X - halfSize, center.Y - halfSize, _crosshairSize, _crosshairSize);

            // 添加中心点
            using (var brush = new SolidBrush(pen.Color))
            {
                g.FillEllipse(brush, center.X - 2, center.Y - 2, 4, 4);
            }
        }

        private void DrawDiagonalCrosshair(Graphics g, Pen pen, Point center)
        {
            var halfSize = _crosshairSize / 2;
            g.DrawLine(pen, center.X - halfSize, center.Y - halfSize, center.X + halfSize, center.Y + halfSize);
            g.DrawLine(pen, center.X + halfSize, center.Y - halfSize, center.X - halfSize, center.Y + halfSize);
        }

        private void DrawCustomCrosshair(Graphics g, Pen pen, Point center)
        {
            // 自定义样式：组合多种元素
            DrawCircleCrosshair(g, pen, center);

            // 添加额外装饰
            using (var brush = new SolidBrush(Color.FromArgb((int)(_crosshairOpacity * 128), _crosshairColor)))
            {
                g.FillEllipse(brush, center.X - 3, center.Y - 3, 6, 6);
            }
        }

        #endregion

        #region 事件处理程序

        private void OnTargetControlMouseMove(object sender, MouseEventArgs e)
        {
            if (_showCrosshair)
            {
                CrosshairPosition = e.Location;
            }
        }

        private void OnTargetControlResize(object sender, EventArgs e)
        {
            if (_showCrosshair)
            {
                CleanupCrosshairBuffer();
                InitializeCrosshairBuffer();
            }
        }

        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            if (_currentAnimation != null)
            {
                _animationFrame = (_animationFrame + 1) % _currentAnimation.FrameCount;

                // 根据动画帧更新光标
                if (_animationFrame == 0)
                {
                    UpdateCursor(); // 循环动画
                }
            }
        }

        #endregion

        #region 事件触发方法

        protected virtual void OnCursorChanged(CursorChangedEventArgs e)
        {
            CursorChanged?.Invoke(this, e);
        }

        protected virtual void OnCrosshairStateChanged(CrosshairStateChangedEventArgs e)
        {
            CrosshairStateChanged?.Invoke(this, e);
        }

        #endregion

        #region IDisposable实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                DetachEvents();

                _animationTimer?.Stop();
                _animationTimer?.Dispose();

                CleanupCrosshairBuffer();

                foreach (var cursor in _customCursors.Values)
                {
                    cursor?.Dispose();
                }
                _customCursors.Clear();

                _targetControl.Cursor = _originalCursor;

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 光标类型
    /// </summary>
    public enum CursorType
    {
        Default,
        Crosshair,
        Hand,
        Help,
        Wait,
        No,
        SizeAll,
        SizeNESW,
        SizeNS,
        SizeNWSE,
        SizeWE,
        UpArrow,
        IBeam,
        Measuring,
        Drawing,
        Selecting,
        Grabbing,
        ZoomIn,
        ZoomOut
    }

    /// <summary>
    /// 十字准星样式
    /// </summary>
    public enum CrosshairStyle
    {
        Simple,     // 简单十字
        Full,       // 全屏十字
        Dashed,     // 虚线十字
        Circle,     // 圆形十字
        Plus,       // 加号十字
        Rectangle,  // 矩形十字
        Diagonal,   // 对角十字
        Custom      // 自定义样式
    }

    /// <summary>
    /// 光标动画
    /// </summary>
    internal class CursorAnimation
    {
        public CursorType[] Frames { get; set; }
        public int FrameCount => Frames?.Length ?? 0;
        public int FrameDuration { get; set; } = 100; // 毫秒
        public bool Loop { get; set; } = true;
    }

    /// <summary>
    /// 光标改变事件参数
    /// </summary>
    public class CursorChangedEventArgs : EventArgs
    {
        public CursorType PreviousCursor { get; }
        public CursorType CurrentCursor { get; }
        public DateTime ChangedAt { get; }

        public CursorChangedEventArgs(CursorType previousCursor, CursorType currentCursor)
        {
            PreviousCursor = previousCursor;
            CurrentCursor = currentCursor;
            ChangedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 十字准星状态改变事件参数
    /// </summary>
    public class CrosshairStateChangedEventArgs : EventArgs
    {
        public bool IsVisible { get; }
        public Point Position { get; }
        public DateTime ChangedAt { get; }

        public CrosshairStateChangedEventArgs(bool isVisible, Point position)
        {
            IsVisible = isVisible;
            Position = position;
            ChangedAt = DateTime.Now;
        }
    }

    #endregion
}