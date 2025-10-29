using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Core;

namespace BoshenCC.WinForms.Utils
{
    /// <summary>
    /// 鼠标交互处理器
    /// 提供增强的鼠标交互功能，包括高级状态跟踪、手势识别、拖拽管理等
    /// </summary>
    public class MouseInteractionHandler : IDisposable
    {
        #region 私有字段

        private readonly Control _targetControl;
        private MouseState _currentState;
        private MouseState _previousState;
        private Point _lastPosition;
        private Point _startPosition;
        private DateTime _lastMoveTime;
        private DateTime _clickTime;
        private int _clickCount;
        private readonly List<Point> _mouseTrail;
        private readonly List<MouseGesture> _gestures;
        private MouseGesture _currentGesture;
        private bool _isDragging;
        private bool _isDoubleClick;
        private readonly Timer _gestureTimer;
        private readonly Timer _clickTimer;
        private readonly object _lockObject = new object();
        private bool _disposed;

        // 配置参数
        private int _gestureThreshold = 50;
        private int _clickTimeout = 500;
        private int _doubleClickTimeout = 500;
        private int _gestureTimeout = 1000;
        private int _maxTrailLength = 50;
        private int _dragThreshold = 5;

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
        /// 拖拽开始事件
        /// </summary>
        public event EventHandler<DragStartEventArgs> DragStart;

        /// <summary>
        /// 拖拽进行中事件
        /// </summary>
        public event EventHandler<DragEventArgs> Dragging;

        /// <summary>
        /// 拖拽结束事件
        /// </summary>
        public event EventHandler<DragEndEventArgs> DragEnd;

        /// <summary>
        /// 单击事件
        /// </summary>
        public event EventHandler<MouseClickEventArgs> SingleClick;

        /// <summary>
        /// 双击事件
        /// </summary>
        public event EventHandler<MouseClickEventArgs> DoubleClick;

        /// <summary>
        /// 右键单击事件
        /// </summary>
        public event EventHandler<MouseClickEventArgs> RightClick;

        /// <summary>
        /// 鼠标移动事件（增强版）
        /// </summary>
        public event EventHandler<EnhancedMouseEventArgs> MouseMoveEnhanced;

        /// <summary>
        /// 鼠标滚轮事件（增强版）
        /// </summary>
        public event EventHandler<EnhancedMouseEventArgs> MouseWheelEnhanced;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化MouseInteractionHandler类
        /// </summary>
        /// <param name="targetControl">目标控件</param>
        public MouseInteractionHandler(Control targetControl)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));

            _currentState = new MouseState();
            _previousState = new MouseState();
            _mouseTrail = new List<Point>();
            _gestures = new List<MouseGesture>();

            InitializeGestures();
            InitializeTimers();
            AttachEvents();
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
        /// 是否正在拖拽
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// 当前手势
        /// </summary>
        public MouseGesture CurrentGesture => _currentGesture;

        /// <summary>
        /// 鼠标轨迹点集合
        /// </summary>
        public IReadOnlyList<Point> MouseTrail => _mouseTrail.AsReadOnly();

        /// <summary>
        /// 手势识别阈值
        /// </summary>
        public int GestureThreshold
        {
            get => _gestureThreshold;
            set => _gestureThreshold = Math.Max(10, value);
        }

        /// <summary>
        /// 点击超时时间（毫秒）
        /// </summary>
        public int ClickTimeout
        {
            get => _clickTimeout;
            set => _clickTimeout = Math.Max(100, value);
        }

        /// <summary>
        /// 双击超时时间（毫秒）
        /// </summary>
        public int DoubleClickTimeout
        {
            get => _doubleClickTimeout;
            set => _doubleClickTimeout = Math.Max(200, value);
        }

        /// <summary>
        /// 手势超时时间（毫秒）
        /// </summary>
        public int GestureTimeout
        {
            get => _gestureTimeout;
            set => _gestureTimeout = Math.Max(500, value);
        }

        /// <summary>
        /// 拖拽阈值（像素）
        /// </summary>
        public int DragThreshold
        {
            get => _dragThreshold;
            set => _dragThreshold = Math.Max(1, value);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加自定义手势
        /// </summary>
        /// <param name="gesture">手势定义</param>
        public void AddGesture(MouseGesture gesture)
        {
            if (gesture == null)
                throw new ArgumentNullException(nameof(gesture));

            lock (_lockObject)
            {
                _gestures.Add(gesture);
            }
        }

        /// <summary>
        /// 移除手势
        /// </summary>
        /// <param name="gestureName">手势名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGesture(string gestureName)
        {
            if (string.IsNullOrEmpty(gestureName))
                return false;

            lock (_lockObject)
            {
                var gesture = _gestures.FirstOrDefault(g => g.Name == gestureName);
                if (gesture != null)
                {
                    return _gestures.Remove(gesture);
                }
                return false;
            }
        }

        /// <summary>
        /// 获取所有手势
        /// </summary>
        /// <returns>手势列表</returns>
        public IReadOnlyList<MouseGesture> GetAllGestures()
        {
            lock (_lockObject)
            {
                return _gestures.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// 清除鼠标轨迹
        /// </summary>
        public void ClearMouseTrail()
        {
            lock (_lockObject)
            {
                _mouseTrail.Clear();
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                _currentState = new MouseState();
                _previousState = new MouseState();
                _isDragging = false;
                _currentGesture = null;
                _clickCount = 0;
                _isDoubleClick = false;
                ClearMouseTrail();
                _gestureTimer.Stop();
                _clickTimer.Stop();
            }
        }

        /// <summary>
        /// 获取鼠标移动速度
        /// </summary>
        /// <returns>移动速度（像素/秒）</returns>
        public double GetMouseSpeed()
        {
            lock (_lockObject)
            {
                if (_mouseTrail.Count < 2)
                    return 0;

                var recent = _mouseTrail.TakeLast(10).ToList();
                if (recent.Count < 2)
                    return 0;

                double totalDistance = 0;
                var totalTime = TimeSpan.Zero;

                for (int i = 1; i < recent.Count; i++)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(recent[i].X - recent[i - 1].X, 2) +
                        Math.Pow(recent[i].Y - recent[i - 1].Y, 2));

                    totalDistance += distance;
                    // 注意：这里简化了时间计算，实际实现中需要记录每个点的时间戳
                    totalTime += TimeSpan.FromMilliseconds(16); // 假设60FPS
                }

                return totalTime.TotalSeconds > 0 ? totalDistance / totalTime.TotalSeconds : 0;
            }
        }

        /// <summary>
        /// 获取鼠标移动方向
        /// </summary>
        /// <returns>移动方向</returns>
        public MouseDirection GetMouseDirection()
        {
            lock (_lockObject)
            {
                if (_mouseTrail.Count < 2)
                    return MouseDirection.None;

                var current = _mouseTrail.Last();
                var previous = _mouseTrail[_mouseTrail.Count - 2];

                var dx = current.X - previous.X;
                var dy = current.Y - previous.Y;

                if (Math.Abs(dx) < 3 && Math.Abs(dy) < 3)
                    return MouseDirection.None;

                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    return dx > 0 ? MouseDirection.Right : MouseDirection.Left;
                }
                else
                {
                    return dy > 0 ? MouseDirection.Down : MouseDirection.Up;
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化默认手势
        /// </summary>
        private void InitializeGestures()
        {
            // 水平向右滑动
            _gestures.Add(new MouseGesture
            {
                Name = "SwipeRight",
                Pattern = new[] { MouseDirection.Right },
                MinDistance = _gestureThreshold,
                MaxTime = TimeSpan.FromMilliseconds(_gestureTimeout),
                Description = "向右滑动"
            });

            // 水平向左滑动
            _gestures.Add(new MouseGesture
            {
                Name = "SwipeLeft",
                Pattern = new[] { MouseDirection.Left },
                MinDistance = _gestureThreshold,
                MaxTime = TimeSpan.FromMilliseconds(_gestureTimeout),
                Description = "向左滑动"
            });

            // 垂直向上滑动
            _gestures.Add(new MouseGesture
            {
                Name = "SwipeUp",
                Pattern = new[] { MouseDirection.Up },
                MinDistance = _gestureThreshold,
                MaxTime = TimeSpan.FromMilliseconds(_gestureTimeout),
                Description = "向上滑动"
            });

            // 垂直向下滑动
            _gestures.Add(new MouseGesture
            {
                Name = "SwipeDown",
                Pattern = new[] { MouseDirection.Down },
                MinDistance = _gestureThreshold,
                MaxTime = TimeSpan.FromMilliseconds(_gestureTimeout),
                Description = "向下滑动"
            });

            // L形手势（右下）
            _gestures.Add(new MouseGesture
            {
                Name = "LShapeRightDown",
                Pattern = new[] { MouseDirection.Right, MouseDirection.Down },
                MinDistance = _gestureThreshold / 2,
                MaxTime = TimeSpan.FromMilliseconds(_gestureTimeout * 2),
                Description = "L形手势（右下）"
            });

            // 圆形手势
            _gestures.Add(new MouseGesture
            {
                Name = "Circle",
                Pattern = new[] {
                    MouseDirection.Right, MouseDirection.Down,
                    MouseDirection.Left, MouseDirection.Up
                },
                MinDistance = _gestureThreshold,
                MaxTime = TimeSpan.FromMilliseconds(_gestureTimeout * 2),
                Description = "圆形手势"
            });
        }

        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitializeTimers()
        {
            _gestureTimer = new Timer
            {
                Interval = _gestureTimeout
            };
            _gestureTimer.Tick += OnGestureTimerTick;

            _clickTimer = new Timer
            {
                Interval = _doubleClickTimeout
            };
            _clickTimer.Tick += OnClickTimerTick;
        }

        /// <summary>
        /// 附加事件处理程序
        /// </summary>
        private void AttachEvents()
        {
            _targetControl.MouseDown += OnMouseDown;
            _targetControl.MouseUp += OnMouseUp;
            _targetControl.MouseMove += OnMouseMove;
            _targetControl.MouseWheel += OnMouseWheel;
            _targetControl.MouseClick += OnMouseClick;
            _targetControl.MouseDoubleClick += OnMouseDoubleClick;
        }

        /// <summary>
        /// 分离事件处理程序
        /// </summary>
        private void DetachEvents()
        {
            _targetControl.MouseDown -= OnMouseDown;
            _targetControl.MouseUp -= OnMouseUp;
            _targetControl.MouseMove -= OnMouseMove;
            _targetControl.MouseWheel -= OnMouseWheel;
            _targetControl.MouseClick -= OnMouseClick;
            _targetControl.MouseDoubleClick -= OnMouseDoubleClick;
        }

        /// <summary>
        /// 更新鼠标轨迹
        /// </summary>
        /// <param name="position">鼠标位置</param>
        private void UpdateMouseTrail(Point position)
        {
            _mouseTrail.Add(position);

            if (_mouseTrail.Count > _maxTrailLength)
            {
                _mouseTrail.RemoveAt(0);
            }
        }

        /// <summary>
        /// 识别手势
        /// </summary>
        private void RecognizeGesture()
        {
            if (_mouseTrail.Count < 2)
                return;

            var directions = ExtractDirections();
            if (directions.Count == 0)
                return;

            lock (_lockObject)
            {
                foreach (var gesture in _gestures)
                {
                    if (IsGestureMatch(gesture, directions))
                    {
                        _currentGesture = gesture;
                        OnGestureRecognized(new GestureRecognizedEventArgs(gesture, _startPosition, _lastPosition));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 从鼠标轨迹提取方向
        /// </summary>
        /// <returns>方向列表</returns>
        private List<MouseDirection> ExtractDirections()
        {
            var directions = new List<MouseDirection>();

            for (int i = 1; i < _mouseTrail.Count; i++)
            {
                var dx = _mouseTrail[i].X - _mouseTrail[i - 1].X;
                var dy = _mouseTrail[i].Y - _mouseTrail[i - 1].Y;

                if (Math.Abs(dx) < 5 && Math.Abs(dy) < 5)
                    continue;

                MouseDirection direction;
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    direction = dx > 0 ? MouseDirection.Right : MouseDirection.Left;
                }
                else
                {
                    direction = dy > 0 ? MouseDirection.Down : MouseDirection.Up;
                }

                // 避免重复的方向
                if (directions.Count == 0 || directions.Last() != direction)
                {
                    directions.Add(direction);
                }
            }

            return directions;
        }

        /// <summary>
        /// 检查手势是否匹配
        /// </summary>
        /// <param name="gesture">手势定义</param>
        /// <param name="directions">检测到的方向</param>
        /// <returns>是否匹配</returns>
        private bool IsGestureMatch(MouseGesture gesture, List<MouseDirection> directions)
        {
            if (directions.Count < gesture.Pattern.Length)
                return false;

            // 检查距离
            var distance = CalculateTotalDistance();
            if (distance < gesture.MinDistance)
                return false;

            // 检查时间
            var elapsedTime = DateTime.Now - _clickTime;
            if (elapsedTime > gesture.MaxTime)
                return false;

            // 检查方向模式
            for (int i = 0; i < gesture.Pattern.Length; i++)
            {
                if (i >= directions.Count || directions[i] != gesture.Pattern[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 计算总移动距离
        /// </summary>
        /// <returns>总距离</returns>
        private double CalculateTotalDistance()
        {
            if (_mouseTrail.Count < 2)
                return 0;

            double totalDistance = 0;
            for (int i = 1; i < _mouseTrail.Count; i++)
            {
                var dx = _mouseTrail[i].X - _mouseTrail[i - 1].X;
                var dy = _mouseTrail[i].Y - _mouseTrail[i - 1].Y;
                totalDistance += Math.Sqrt(dx * dx + dy * dy);
            }

            return totalDistance;
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        private void StartDrag()
        {
            if (_isDragging)
                return;

            _isDragging = true;
            OnDragStart(new DragStartEventArgs(_startPosition, _lastPosition));
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        private void EndDrag()
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            OnDragEnd(new DragEndEventArgs(_startPosition, _lastPosition, CalculateTotalDistance()));
        }

        #endregion

        #region 事件处理程序

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            lock (_lockObject)
            {
                _previousState = _currentState;
                _currentState = new MouseState
                {
                    Position = e.Location,
                    Buttons = e.Button,
                    Timestamp = DateTime.Now,
                    IsPressed = true,
                    ClickCount = _clickCount + 1
                };

                _startPosition = e.Location;
                _lastPosition = e.Location;
                _clickTime = DateTime.Now;
                _clickCount++;

                // 停止之前的定时器
                _gestureTimer.Stop();
                _clickTimer.Stop();
                _clickTimer.Start();

                OnMouseStateChanged(new MouseStateChangedEventArgs(_previousState, _currentState));
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            lock (_lockObject)
            {
                _previousState = _currentState;
                _currentState = new MouseState
                {
                    Position = e.Location,
                    Buttons = MouseButtons.None,
                    Timestamp = DateTime.Now,
                    IsPressed = false,
                    ClickCount = _clickCount
                };

                _lastPosition = e.Location;

                // 检查是否是拖拽结束
                if (_isDragging)
                {
                    EndDrag();
                }
                else
                {
                    // 检查手势
                    RecognizeGesture();
                    _gestureTimer.Start();
                }

                OnMouseStateChanged(new MouseStateChangedEventArgs(_previousState, _currentState));
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            lock (_lockObject)
            {
                _previousState = _currentState;
                _lastPosition = e.Location;
                _lastMoveTime = DateTime.Now;

                UpdateMouseTrail(e.Location);

                _currentState = new MouseState
                {
                    Position = e.Location,
                    Buttons = e.Button,
                    Timestamp = DateTime.Now,
                    IsPressed = e.Button != MouseButtons.None,
                    ClickCount = _clickCount,
                    Speed = GetMouseSpeed(),
                    Direction = GetMouseDirection()
                };

                // 检查是否应该开始拖拽
                if (_currentState.IsPressed && !_isDragging)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(e.Location.X - _startPosition.X, 2) +
                        Math.Pow(e.Location.Y - _startPosition.Y, 2));

                    if (distance > _dragThreshold)
                    {
                        StartDrag();
                    }
                }

                // 拖拽进行中
                if (_isDragging)
                {
                    OnDragging(new DragEventArgs(_startPosition, e.Location, CalculateTotalDistance()));
                }

                OnMouseMoveEnhanced(new EnhancedMouseEventArgs(e));
                OnMouseStateChanged(new MouseStateChangedEventArgs(_previousState, _currentState));
            }
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            OnMouseWheelEnhanced(new EnhancedMouseEventArgs(e));
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                OnSingleClick(new MouseClickEventArgs(e.Location, e.Button, _clickCount));
            }
            else if (e.Button == MouseButtons.Right)
            {
                OnRightClick(new MouseClickEventArgs(e.Location, e.Button, _clickCount));
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDoubleClick = true;
                OnDoubleClick(new MouseClickEventArgs(e.Location, e.Button, _clickCount));
            }
        }

        private void OnGestureTimerTick(object sender, EventArgs e)
        {
            _gestureTimer.Stop();
            RecognizeGesture();
        }

        private void OnClickTimerTick(object sender, EventArgs e)
        {
            _clickTimer.Stop();
            _clickCount = 0;
            _isDoubleClick = false;
        }

        #endregion

        #region 事件触发方法

        protected virtual void OnMouseStateChanged(MouseStateChangedEventArgs e)
        {
            MouseStateChanged?.Invoke(this, e);
        }

        protected virtual void OnGestureRecognized(GestureRecognizedEventArgs e)
        {
            GestureRecognized?.Invoke(this, e);
        }

        protected virtual void OnDragStart(DragStartEventArgs e)
        {
            DragStart?.Invoke(this, e);
        }

        protected virtual void OnDragging(DragEventArgs e)
        {
            Dragging?.Invoke(this, e);
        }

        protected virtual void OnDragEnd(DragEndEventArgs e)
        {
            DragEnd?.Invoke(this, e);
        }

        protected virtual void OnSingleClick(MouseClickEventArgs e)
        {
            SingleClick?.Invoke(this, e);
        }

        protected virtual void OnDoubleClick(MouseClickEventArgs e)
        {
            DoubleClick?.Invoke(this, e);
        }

        protected virtual void OnRightClick(MouseClickEventArgs e)
        {
            RightClick?.Invoke(this, e);
        }

        protected virtual void OnMouseMoveEnhanced(EnhancedMouseEventArgs e)
        {
            MouseMoveEnhanced?.Invoke(this, e);
        }

        protected virtual void OnMouseWheelEnhanced(EnhancedMouseEventArgs e)
        {
            MouseWheelEnhanced?.Invoke(this, e);
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

                _gestureTimer?.Stop();
                _gestureTimer?.Dispose();

                _clickTimer?.Stop();
                _clickTimer?.Dispose();

                _mouseTrail.Clear();
                _gestures.Clear();

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 鼠标状态
    /// </summary>
    public class MouseState
    {
        public Point Position { get; set; }
        public MouseButtons Buttons { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsPressed { get; set; }
        public int ClickCount { get; set; }
        public double Speed { get; set; }
        public MouseDirection Direction { get; set; }
    }

    /// <summary>
    /// 鼠标手势定义
    /// </summary>
    public class MouseGesture
    {
        public string Name { get; set; }
        public MouseDirection[] Pattern { get; set; }
        public int MinDistance { get; set; }
        public TimeSpan MaxTime { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 鼠标方向
    /// </summary>
    public enum MouseDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// 鼠标状态改变事件参数
    /// </summary>
    public class MouseStateChangedEventArgs : EventArgs
    {
        public MouseState PreviousState { get; }
        public MouseState CurrentState { get; }
        public DateTime ChangedAt { get; }

        public MouseStateChangedEventArgs(MouseState previousState, MouseState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            ChangedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 手势识别事件参数
    /// </summary>
    public class GestureRecognizedEventArgs : EventArgs
    {
        public MouseGesture Gesture { get; }
        public Point StartPosition { get; }
        public Point EndPosition { get; }
        public DateTime RecognizedAt { get; }

        public GestureRecognizedEventArgs(MouseGesture gesture, Point startPosition, Point endPosition)
        {
            Gesture = gesture;
            StartPosition = startPosition;
            EndPosition = endPosition;
            RecognizedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 拖拽开始事件参数
    /// </summary>
    public class DragStartEventArgs : EventArgs
    {
        public Point StartPosition { get; }
        public Point CurrentPosition { get; }
        public DateTime StartedAt { get; }

        public DragStartEventArgs(Point startPosition, Point currentPosition)
        {
            StartPosition = startPosition;
            CurrentPosition = currentPosition;
            StartedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 拖拽进行中事件参数
    /// </summary>
    public class DragEventArgs : EventArgs
    {
        public Point StartPosition { get; }
        public Point CurrentPosition { get; }
        public double TotalDistance { get; }
        public DateTime At { get; }

        public DragEventArgs(Point startPosition, Point currentPosition, double totalDistance)
        {
            StartPosition = startPosition;
            CurrentPosition = currentPosition;
            TotalDistance = totalDistance;
            At = DateTime.Now;
        }
    }

    /// <summary>
    /// 拖拽结束事件参数
    /// </summary>
    public class DragEndEventArgs : EventArgs
    {
        public Point StartPosition { get; }
        public Point EndPosition { get; }
        public double TotalDistance { get; }
        public DateTime EndedAt { get; }

        public DragEndEventArgs(Point startPosition, Point endPosition, double totalDistance)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            TotalDistance = totalDistance;
            EndedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 鼠标点击事件参数
    /// </summary>
    public class MouseClickEventArgs : EventArgs
    {
        public Point Position { get; }
        public MouseButtons Button { get; }
        public int ClickCount { get; }
        public DateTime ClickedAt { get; }

        public MouseClickEventArgs(Point position, MouseButtons button, int clickCount)
        {
            Position = position;
            Button = button;
            ClickCount = clickCount;
            ClickedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 增强的鼠标事件参数
    /// </summary>
    public class EnhancedMouseEventArgs : MouseEventArgs
    {
        public MouseDirection Direction { get; }
        public double Speed { get; }
        public Point PreviousPosition { get; }

        public EnhancedMouseEventArgs(MouseEventArgs originalArgs,
            MouseDirection direction = MouseDirection.None,
            double speed = 0,
            Point? previousPosition = null)
            : base(originalArgs.Button, originalArgs.Clicks, originalArgs.X, originalArgs.Y, originalArgs.Delta)
        {
            Direction = direction;
            Speed = speed;
            PreviousPosition = previousPosition ?? originalArgs.Location;
        }
    }

    #endregion
}