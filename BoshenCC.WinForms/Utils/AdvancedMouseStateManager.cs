using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 高级鼠标状态管理器
    /// 提供丰富的鼠标状态跟踪、手势识别和增强的十字准星功能
    /// </summary>
    public class AdvancedMouseStateManager : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        #region 私有字段

        private readonly Control _targetControl;
        private readonly List<MouseTrail> _mouseTrails;
        private readonly List<MouseGesture> _gestures;
        private readonly CrosshairManager _crosshairManager;
        private readonly Timer _updateTimer;
        private readonly Timer _gestureTimer;

        private MouseState _currentState;
        private MouseState _previousState;
        private Point _currentPosition;
        private Point _lastPosition;
        private Point _startPosition;
        private DateTime _lastMoveTime;
        private DateTime _startTime;
        private DateTime _lastClickTime;
        private int _clickCount;
        private MouseButtons _lastClickButton;
        private bool _isDragging;
        private bool _isGestureActive;
        private Rectangle _dragBounds;
        private Cursor _customCursor;

        private double _totalDistance;
        private double _currentSpeed;
        private Vector2D _currentDirection;
        private double _acceleration;

        #endregion

        #region 枚举

        /// <summary>
        /// 鼠标状态
        /// </summary>
        public enum MouseState
        {
            Idle,               // 空闲
            Hovering,           // 悬停
            Moving,             // 移动
            Dragging,           // 拖拽
            Clicking,           // 点击
            DoubleClicking,     // 双击
            Gesturing,          // 手势识别
            Scrolling,          // 滚动
            RightClicking,      // 右键点击
            WheelScrolling      // 滚轮滚动
        }

        /// <summary>
        /// 手势类型
        /// </summary>
        public enum GestureType
        {
            None,
            LeftSwipe,
            RightSwipe,
            UpSwipe,
            DownSwipe,
            Circle,
            DiagonalUpLeft,
            DiagonalUpRight,
            DiagonalDownLeft,
            DiagonalDownRight,
            ZoomIn,
            ZoomOut
        }

        #endregion

        #region 构造函数

        public AdvancedMouseStateManager(Control targetControl)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));

            _mouseTrails = new List<MouseTrail>();
            _gestures = new List<MouseGesture>();
            _crosshairManager = new CrosshairManager();

            _currentState = MouseState.Idle;
            _previousState = MouseState.Idle;
            _currentPosition = Point.Empty;
            _lastPosition = Point.Empty;
            _startPosition = Point.Empty;
            _lastMoveTime = DateTime.Now;
            _startTime = DateTime.Now;
            _lastClickTime = DateTime.MinValue;
            _clickCount = 0;
            _lastClickButton = MouseButtons.None;
            _isDragging = false;
            _isGestureActive = false;

            _totalDistance = 0;
            _currentSpeed = 0;
            _currentDirection = new Vector2D(0, 0);
            _acceleration = 0;

            _updateTimer = new Timer { Interval = 16 }; // ~60 FPS
            _updateTimer.Tick += UpdateTimer_Tick;

            _gestureTimer = new Timer { Interval = 500 }; // 手势超时
            _gestureTimer.Tick += GestureTimer_Tick;

            InitializeEventHandlers();
            InitializeGestures();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前鼠标状态
        /// </summary>
        public MouseState CurrentState => _currentState;

        /// <summary>
        /// 前一个鼠标状态
        /// </summary>
        public MouseState PreviousState => _previousState;

        /// <summary>
        /// 当前鼠标位置（控件坐标）
        /// </summary>
        public Point CurrentPosition => _currentPosition;

        /// <summary>
        /// 鼠标移动速度（像素/秒）
        /// </summary>
        public double CurrentSpeed => _currentSpeed;

        /// <summary>
        /// 鼠标移动方向
        /// </summary>
        public Vector2D CurrentDirection => _currentDirection;

        /// <summary>
        /// 鼠标加速度
        /// </summary>
        public double Acceleration => _acceleration;

        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// 是否正在识别手势
        /// </summary>
        public bool IsGestureActive => _isGestureActive;

        /// <summary>
        /// 拖拽边界
        /// </summary>
        public Rectangle DragBounds => _dragBounds;

        /// <summary>
        /// 点击计数
        /// </summary>
        public int ClickCount => _clickCount;

        /// <summary>
        /// 总移动距离
        /// </summary>
        public double TotalDistance => _totalDistance;

        /// <summary>
        /// 十字准星管理器
        /// </summary>
        public CrosshairManager CrosshairManager => _crosshairManager;

        /// <summary>
        /// 自定义光标
        /// </summary>
        public Cursor CustomCursor
        {
            get => _customCursor;
            set
            {
                _customCursor = value;
                if (value != null)
                {
                    _targetControl.Cursor = value;
                }
            }
        }

        /// <summary>
        /// 是否启用鼠标轨迹
        /// </summary>
        public bool EnableMouseTrails { get; set; } = true;

        /// <summary>
        /// 是否启用手势识别
        /// </summary>
        public bool EnableGestureRecognition { get; set; } = true;

        /// <summary>
        /// 轨迹最大长度
        /// </summary>
        public int MaxTrailLength { get; set; } = 30;

        #endregion

        #region 事件

        /// <summary>
        /// 鼠标状态改变事件
        /// </summary>
        public event EventHandler<MouseStateChangedEventArgs> MouseStateChanged;

        /// <summary>
        /// 手势识别事件
        /// </summary>
        public event EventHandler<GestureRecognizedEventArgs> GestureRecognized;

        /// <summary>
        /// 鼠标活动事件
        /// </summary>
        public event EventHandler<MouseActivityEventArgs> MouseActivity;

        /// <summary>
        /// 拖拽开始事件
        /// </summary>
        public event EventHandler<DragStartedEventArgs> DragStarted;

        /// <summary>
        /// 拖拽结束事件
        /// </summary>
        public event EventHandler<DragEndedEventArgs> DragEnded;

        /// <summary>
        /// 快速移动事件
        /// </summary>
        public event EventHandler<QuickMoveEventArgs> QuickMove;

        #endregion

        #region 公共方法

        /// <summary>
        /// 启用鼠标状态管理
        /// </summary>
        public void Enable()
        {
            _updateTimer.Start();
        }

        /// <summary>
        /// 禁用鼠标状态管理
        /// </summary>
        public void Disable()
        {
            _updateTimer.Stop();
            _gestureTimer.Stop();
        }

        /// <summary>
        /// 清除鼠标轨迹
        /// </summary>
        public void ClearTrails()
        {
            _mouseTrails.Clear();
        }

        /// <summary>
        /// 添加手势识别器
        /// </summary>
        /// <param name="gesture">手势</param>
        public void AddGesture(MouseGesture gesture)
        {
            if (gesture != null && !_gestures.Contains(gesture))
            {
                _gestures.Add(gesture);
            }
        }

        /// <summary>
        /// 移除手势识别器
        /// </summary>
        /// <param name="gesture">手势</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveGesture(MouseGesture gesture)
        {
            return _gestures.Remove(gesture);
        }

        /// <summary>
        /// 清除所有手势
        /// </summary>
        public void ClearGestures()
        {
            _gestures.Clear();
        }

        /// <summary>
        /// 获取鼠标轨迹点
        /// </summary>
        /// <returns>轨迹点列表</returns>
        public List<Point> GetTrailPoints()
        {
            return _mouseTrails.Select(t => t.Position).ToList();
        }

        /// <summary>
        /// 获取指定时间内的轨迹
        /// </summary>
        /// <param name="timeSpan">时间范围</param>
        /// <returns>轨迹点列表</returns>
        public List<Point> GetTrailPoints(TimeSpan timeSpan)
        {
            var cutoff = DateTime.Now - timeSpan;
            return _mouseTrails
                .Where(t => t.Timestamp >= cutoff)
                .Select(t => t.Position)
                .ToList();
        }

        /// <summary>
        /// 强制更新鼠标状态
        /// </summary>
        public void ForceUpdate()
        {
            UpdateMouseState();
            CalculateMovementMetrics();
        }

        /// <summary>
        /// 绘制鼠标轨迹和十字准星
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="bounds">绘制边界</param>
        public void Draw(Graphics graphics, Rectangle bounds)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制鼠标轨迹
            if (EnableMouseTrails)
            {
                DrawMouseTrails(graphics);
            }

            // 绘制十字准星
            _crosshairManager.Draw(graphics, bounds, _currentPosition);

            // 绘制拖拽框
            if (_isDragging && !_dragBounds.IsEmpty)
            {
                DrawDragBounds(graphics);
            }

            // 绘制手势路径
            if (_isGestureActive)
            {
                DrawGesturePath(graphics);
            }
        }

        /// <summary>
        /// 模拟鼠标移动
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="animated">是否使用动画</param>
        public void SimulateMouseMove(Point targetPosition, bool animated = true)
        {
            var screenPos = _targetControl.PointToScreen(targetPosition);

            if (animated)
            {
                // 简单的线性插值动画
                var currentScreenPos = Cursor.Position;
                var steps = 10;
                var dx = (screenPos.X - currentScreenPos.X) / (float)steps;
                var dy = (screenPos.Y - currentScreenPos.Y) / (float)steps;

                for (int i = 1; i <= steps; i++)
                {
                    var x = currentScreenPos.X + dx * i;
                    var y = currentScreenPos.Y + dy * i;
                    SetCursorPos((int)x, (int)y);
                    System.Threading.Thread.Sleep(10);
                }
            }
            else
            {
                SetCursorPos(screenPos.X, screenPos.Y);
            }
        }

        #endregion

        #region 私有方法

        private void InitializeEventHandlers()
        {
            _targetControl.MouseMove += TargetControl_MouseMove;
            _targetControl.MouseDown += TargetControl_MouseDown;
            _targetControl.MouseUp += TargetControl_MouseUp;
            _targetControl.MouseClick += TargetControl_MouseClick;
            _targetControl.MouseDoubleClick += TargetControl_MouseDoubleClick;
            _targetControl.MouseEnter += TargetControl_MouseEnter;
            _targetControl.MouseLeave += TargetControl_MouseLeave;
            _targetControl.MouseWheel += TargetControl_MouseWheel;
        }

        private void InitializeGestures()
        {
            // 添加基本手势
            AddGesture(new MouseGesture(GestureType.LeftSwipe, new List<Point> { new Point(0, 0), new Point(-50, 0) }));
            AddGesture(new MouseGesture(GestureType.RightSwipe, new List<Point> { new Point(0, 0), new Point(50, 0) }));
            AddGesture(new MouseGesture(GestureType.UpSwipe, new List<Point> { new Point(0, 0), new Point(0, -50) }));
            AddGesture(new MouseGesture(GestureType.DownSwipe, new List<Point> { new Point(0, 0), new Point(0, 50) }));
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateMouseState();
            CalculateMovementMetrics();
            UpdateTrails();
        }

        private void GestureTimer_Tick(object sender, EventArgs e)
        {
            if (_isGestureActive)
            {
                EndGesture();
            }
            _gestureTimer.Stop();
        }

        private void TargetControl_MouseMove(object sender, MouseEventArgs e)
        {
            _lastPosition = _currentPosition;
            _currentPosition = e.Location;
            _lastMoveTime = DateTime.Now;

            if (_isDragging)
            {
                UpdateDragBounds();
            }

            if (_isGestureActive)
            {
                UpdateGesture();
            }

            OnMouseActivity(new MouseActivityEventArgs(e.Location, _currentSpeed, _currentState));
        }

        private void TargetControl_MouseDown(object sender, MouseEventArgs e)
        {
            _startPosition = e.Location;
            _startTime = DateTime.Now;

            if (e.Button == MouseButtons.Left)
            {
                StartDrag(e.Location);
                StartGesture(e.Location);
            }

            ChangeState(MouseState.Clicking);
        }

        private void TargetControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                EndDrag();
                if (_isGestureActive)
                {
                    RecognizeGesture();
                    EndGesture();
                }
            }

            ChangeState(MouseState.Idle);
        }

        private void TargetControl_MouseClick(object sender, MouseEventArgs e)
        {
            var now = DateTime.Now;
            var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;

            if (timeSinceLastClick < SystemInformation.DoubleClickTime && _lastClickButton == e.Button)
            {
                _clickCount++;
                ChangeState(MouseState.DoubleClicking);
            }
            else
            {
                _clickCount = 1;
            }

            _lastClickTime = now;
            _lastClickButton = e.Button;
        }

        private void TargetControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ChangeState(MouseState.DoubleClicking);
        }

        private void TargetControl_MouseEnter(object sender, EventArgs e)
        {
            ChangeState(MouseState.Hovering);
        }

        private void TargetControl_MouseLeave(object sender, EventArgs e)
        {
            ChangeState(MouseState.Idle);
            ClearTrails();
        }

        private void TargetControl_MouseWheel(object sender, MouseEventArgs e)
        {
            ChangeState(MouseState.WheelScrolling);
        }

        private void UpdateMouseState()
        {
            var now = DateTime.Now;
            var timeSinceLastMove = (now - _lastMoveTime).TotalMilliseconds;

            MouseState newState;

            if (_isDragging)
            {
                newState = MouseState.Dragging;
            }
            else if (_isGestureActive)
            {
                newState = MouseState.Gesturing;
            }
            else if (timeSinceLastMove > 100)
            {
                newState = _targetControl.ClientRectangle.Contains(_currentPosition) ? MouseState.Hovering : MouseState.Idle;
            }
            else if (_currentSpeed > 500)
            {
                newState = MouseState.Moving;
                OnQuickMove(new QuickMoveEventArgs(_currentPosition, _currentSpeed, _currentDirection));
            }
            else
            {
                newState = MouseState.Hovering;
            }

            ChangeState(newState);
        }

        private void CalculateMovementMetrics()
        {
            if (_lastPosition == Point.Empty || _currentPosition == Point.Empty)
                return;

            var now = DateTime.Now;
            var timeDelta = (now - _lastMoveTime).TotalSeconds;

            if (timeDelta > 0)
            {
                var distanceDelta = Math.Sqrt(
                    Math.Pow(_currentPosition.X - _lastPosition.X, 2) +
                    Math.Pow(_currentPosition.Y - _lastPosition.Y, 2));

                _totalDistance += distanceDelta;

                var newSpeed = distanceDelta / timeDelta;
                _acceleration = (newSpeed - _currentSpeed) / timeDelta;
                _currentSpeed = newSpeed;

                if (distanceDelta > 0)
                {
                    _currentDirection = new Vector2D(
                        _currentPosition.X - _lastPosition.X,
                        _currentPosition.Y - _lastPosition.Y).Normalized();
                }
            }
        }

        private void UpdateTrails()
        {
            if (!EnableMouseTrails) return;

            _mouseTrails.Add(new MouseTrail(_currentPosition, DateTime.Now, _currentSpeed));

            // 限制轨迹长度
            while (_mouseTrails.Count > MaxTrailLength)
            {
                _mouseTrails.RemoveAt(0);
            }
        }

        private void ChangeState(MouseState newState)
        {
            if (_currentState != newState)
            {
                _previousState = _currentState;
                _currentState = newState;
                OnMouseStateChanged(new MouseStateChangedEventArgs(_previousState, _currentState, _currentPosition));
            }
        }

        private void StartDrag(Point position)
        {
            _isDragging = true;
            _dragBounds = new Rectangle(position, Size.Empty);
            OnDragStarted(new DragStartedEventArgs(position));
        }

        private void UpdateDragBounds()
        {
            if (_isDragging)
            {
                _dragBounds = Rectangle.FromLTRB(
                    Math.Min(_startPosition.X, _currentPosition.X),
                    Math.Min(_startPosition.Y, _currentPosition.Y),
                    Math.Max(_startPosition.X, _currentPosition.X),
                    Math.Max(_startPosition.Y, _currentPosition.Y));
            }
        }

        private void EndDrag()
        {
            if (_isDragging)
            {
                _isDragging = false;
                OnDragEnded(new DragEndedEventArgs(_startPosition, _currentPosition, _dragBounds));
                _dragBounds = Rectangle.Empty;
            }
        }

        private void StartGesture(Point position)
        {
            if (!EnableGestureRecognition) return;

            _isGestureActive = true;
            _gestureTimer.Start();
        }

        private void UpdateGesture()
        {
            // 手势更新逻辑在识别时处理
        }

        private void EndGesture()
        {
            _isGestureActive = false;
            _gestureTimer.Stop();
        }

        private void RecognizeGesture()
        {
            var gesturePoints = GetTrailPoints(TimeSpan.FromMilliseconds(500));
            if (gesturePoints.Count < 2) return;

            foreach (var gesture in _gestures)
            {
                if (gesture.IsMatch(gesturePoints))
                {
                    OnGestureRecognized(new GestureRecognizedEventArgs(gesture.Type, gesturePoints));
                    break;
                }
            }
        }

        private void DrawMouseTrails(Graphics g)
        {
            if (_mouseTrails.Count < 2) return;

            for (int i = 1; i < _mouseTrails.Count; i++)
            {
                var trail = _mouseTrails[i];
                var prevTrail = _mouseTrails[i - 1];

                var alpha = (int)(255 * (i / (float)_mouseTrails.Count));
                var color = Color.FromArgb(alpha, Color.Orange);
                var width = Math.Max(1, (int)(trail.Speed / 100));

                using (var pen = new Pen(color, width))
                {
                    g.DrawLine(pen, prevTrail.Position, trail.Position);
                }
            }
        }

        private void DrawDragBounds(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(50, Color.Blue)))
            using (var pen = new Pen(Color.Blue, 2))
            {
                g.FillRectangle(brush, _dragBounds);
                g.DrawRectangle(pen, _dragBounds);
            }

            // 绘制尺寸信息
            var sizeText = $"{_dragBounds.Width} × {_dragBounds.Height}";
            using (var font = new Font("Arial", 9))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(sizeText, font, brush, _dragBounds.X + 5, _dragBounds.Y + 5);
            }
        }

        private void DrawGesturePath(Graphics g)
        {
            var gesturePoints = GetTrailPoints(TimeSpan.FromMilliseconds(500));
            if (gesturePoints.Count < 2) return;

            using (var pen = new Pen(Color.Green, 3))
            {
                for (int i = 1; i < gesturePoints.Count; i++)
                {
                    g.DrawLine(pen, gesturePoints[i - 1], gesturePoints[i]);
                }
            }

            // 绘制起点和终点
            if (gesturePoints.Count > 0)
            {
                using (var startBrush = new SolidBrush(Color.Green))
                using (var endBrush = new SolidBrush(Color.Red))
                {
                    g.FillEllipse(startBrush, gesturePoints[0].X - 5, gesturePoints[0].Y - 5, 10, 10);
                    g.FillEllipse(endBrush, gesturePoints[gesturePoints.Count - 1].X - 5,
                        gesturePoints[gesturePoints.Count - 1].Y - 5, 10, 10);
                }
            }
        }

        #endregion

        #region 事件触发器

        protected virtual void OnMouseStateChanged(MouseStateChangedEventArgs e)
        {
            MouseStateChanged?.Invoke(this, e);
        }

        protected virtual void OnGestureRecognized(GestureRecognizedEventArgs e)
        {
            GestureRecognized?.Invoke(this, e);
        }

        protected virtual void OnMouseActivity(MouseActivityEventArgs e)
        {
            MouseActivity?.Invoke(this, e);
        }

        protected virtual void OnDragStarted(DragStartedEventArgs e)
        {
            DragStarted?.Invoke(this, e);
        }

        protected virtual void OnDragEnded(DragEndedEventArgs e)
        {
            DragEnded?.Invoke(this, e);
        }

        protected virtual void OnQuickMove(QuickMoveEventArgs e)
        {
            QuickMove?.Invoke(this, e);
        }

        #endregion

        #region 资源释放

        public void Dispose()
        {
            _updateTimer?.Stop();
            _gestureTimer?.Stop();
            _updateTimer?.Dispose();
            _gestureTimer?.Dispose();
            _crosshairManager?.Dispose();
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 鼠标轨迹
    /// </summary>
    public class MouseTrail
    {
        public Point Position { get; }
        public DateTime Timestamp { get; }
        public double Speed { get; }

        public MouseTrail(Point position, DateTime timestamp, double speed)
        {
            Position = position;
            Timestamp = timestamp;
            Speed = speed;
        }
    }

    /// <summary>
    /// 二维向量
    /// </summary>
    public class Vector2D
    {
        public double X { get; }
        public double Y { get; }

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Magnitude => Math.Sqrt(X * X + Y * Y);
        public Vector2D Normalized => Magnitude > 0 ? new Vector2D(X / Magnitude, Y / Magnitude) : new Vector2D(0, 0);

        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator *(Vector2D v, double scalar) => new Vector2D(v.X * scalar, v.Y * scalar);
    }

    /// <summary>
    /// 鼠标手势
    /// </summary>
    public class MouseGesture
    {
        public AdvancedMouseStateManager.GestureType Type { get; }
        public List<Point> Pattern { get; }
        public double Tolerance { get; set; } = 20.0;

        public MouseGesture(AdvancedMouseStateManager.GestureType type, List<Point> pattern)
        {
            Type = type;
            Pattern = pattern ?? new List<Point>();
        }

        public bool IsMatch(List<Point> points)
        {
            if (points.Count < 2 || Pattern.Count < 2) return false;

            // 简化的手势匹配算法
            var gestureDirection = CalculateDirection(points);
            var patternDirection = CalculateDirection(Pattern);

            return CalculateAngleDifference(gestureDirection, patternDirection) < Tolerance;
        }

        private Vector2D CalculateDirection(List<Point> points)
        {
            if (points.Count < 2) return new Vector2D(0, 0);

            var start = points[0];
            var end = points[points.Count - 1];

            return new Vector2D(end.X - start.X, end.Y - start.Y).Normalized;
        }

        private double CalculateAngleDifference(Vector2D v1, Vector2D v2)
        {
            var dot = v1.X * v2.X + v1.Y * v2.Y;
            var cross = v1.X * v2.Y - v1.Y * v2.X;
            return Math.Atan2(cross, dot) * 180 / Math.PI;
        }
    }

    #endregion

    #region 事件参数类

    /// <summary>
    /// 鼠标状态改变事件参数
    /// </summary>
    public class MouseStateChangedEventArgs : EventArgs
    {
        public AdvancedMouseStateManager.MouseState OldState { get; }
        public AdvancedMouseStateManager.MouseState NewState { get; }
        public Point Position { get; }

        public MouseStateChangedEventArgs(
            AdvancedMouseStateManager.MouseState oldState,
            AdvancedMouseStateManager.MouseState newState,
            Point position)
        {
            OldState = oldState;
            NewState = newState;
            Position = position;
        }
    }

    /// <summary>
    /// 手势识别事件参数
    /// </summary>
    public class GestureRecognizedEventArgs : EventArgs
    {
        public AdvancedMouseStateManager.GestureType GestureType { get; }
        public List<Point> Points { get; }

        public GestureRecognizedEventArgs(AdvancedMouseStateManager.GestureType gestureType, List<Point> points)
        {
            GestureType = gestureType;
            Points = points;
        }
    }

    /// <summary>
    /// 鼠标活动事件参数
    /// </summary>
    public class MouseActivityEventArgs : EventArgs
    {
        public Point Position { get; }
        public double Speed { get; }
        public AdvancedMouseStateManager.MouseState State { get; }

        public MouseActivityEventArgs(Point position, double speed, AdvancedMouseStateManager.MouseState state)
        {
            Position = position;
            Speed = speed;
            State = state;
        }
    }

    /// <summary>
    /// 拖拽开始事件参数
    /// </summary>
    public class DragStartedEventArgs : EventArgs
    {
        public Point StartPosition { get; }

        public DragStartedEventArgs(Point startPosition)
        {
            StartPosition = startPosition;
        }
    }

    /// <summary>
    /// 拖拽结束事件参数
    /// </summary>
    public class DragEndedEventArgs : EventArgs
    {
        public Point StartPosition { get; }
        public Point EndPosition { get; }
        public Rectangle DragBounds { get; }

        public DragEndedEventArgs(Point startPosition, Point endPosition, Rectangle dragBounds)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            DragBounds = dragBounds;
        }
    }

    /// <summary>
    /// 快速移动事件参数
    /// </summary>
    public class QuickMoveEventArgs : EventArgs
    {
        public Point Position { get; }
        public double Speed { get; }
        public Vector2D Direction { get; }

        public QuickMoveEventArgs(Point position, double speed, Vector2D direction)
        {
            Position = position;
            Speed = speed;
            Direction = direction;
        }
    }

    #endregion
}