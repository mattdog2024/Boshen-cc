using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Core;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 手势识别处理器
    /// 提供高级的手势识别功能，支持多种手势类型和自定义手势
    /// </summary>
    public class GestureHandler : IDisposable
    {
        #region 私有字段

        private readonly Control _targetControl;
        private readonly List<GestureDefinition> _gestureDefinitions;
        private readonly List<Point> _gesturePoints;
        private bool _isRecording;
        private DateTime _recordingStartTime;
        private Point _startPoint;
        private Point _currentPoint;
        private readonly Timer _recognitionTimer;
        private bool _disposed;
        private readonly object _lockObject = new object();

        // 手势识别参数
        private int _minGestureLength = 30;
        private int _maxGestureTime = 2000; // 毫秒
        private int _simplificationTolerance = 10;
        private float _recognitionThreshold = 0.7f; // 70% 匹配度

        // 可视化相关
        private bool _showGestureTrail;
        private Color _trailColor = Color.Blue;
        private int _trailWidth = 3;
        private float _trailOpacity = 0.8f;
        private Bitmap _trailBuffer;
        private Graphics _trailGraphics;

        #endregion

        #region 事件

        /// <summary>
        /// 手势开始事件
        /// </summary>
        public event EventHandler<GestureStartEventArgs> GestureStart;

        /// <summary>
        /// 手势进行中事件
        /// </summary>
        public event EventHandler<GestureProgressEventArgs> GestureProgress;

        /// <summary>
        /// 手势结束事件
        /// </summary>
        public event EventHandler<GestureEndEventArgs> GestureEnd;

        /// <summary>
        /// 手势识别成功事件
        /// </summary>
        public event EventHandler<GestureRecognizedEventArgs> GestureRecognized;

        /// <summary>
        /// 手势识别失败事件
        /// </summary>
        public event EventHandler<GestureFailedEventArgs> GestureFailed;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化GestureHandler类
        /// </summary>
        /// <param name="targetControl">目标控件</param>
        public GestureHandler(Control targetControl)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));

            _gestureDefinitions = new List<GestureDefinition>();
            _gesturePoints = new List<Point>();

            InitializeDefaultGestures();
            InitializeRecognitionTimer();
            InitializeVisualization();
            AttachEvents();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否正在录制手势
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// 最小手势长度
        /// </summary>
        public int MinGestureLength
        {
            get => _minGestureLength;
            set => _minGestureLength = Math.Max(10, value);
        }

        /// <summary>
        /// 最大手势时间（毫秒）
        /// </summary>
        public int MaxGestureTime
        {
            get => _maxGestureTime;
            set => _maxGestureTime = Math.Max(500, value);
        }

        /// <summary>
        /// 简化容差
        /// </summary>
        public int SimplificationTolerance
        {
            get => _simplificationTolerance;
            set => _simplificationTolerance = Math.Max(1, value);
        }

        /// <summary>
        /// 识别阈值（0-1）
        /// </summary>
        public float RecognitionThreshold
        {
            get => _recognitionThreshold;
            set => _recognitionThreshold = Math.Max(0.1f, Math.Min(1.0f, value));
        }

        /// <summary>
        /// 是否显示手势轨迹
        /// </summary>
        public bool ShowGestureTrail
        {
            get => _showGestureTrail;
            set
            {
                if (_showGestureTrail != value)
                {
                    _showGestureTrail = value;
                    if (!_showGestureTrail)
                    {
                        ClearTrail();
                    }
                    _targetControl.Invalidate();
                }
            }
        }

        /// <summary>
        /// 轨迹颜色
        /// </summary>
        public Color TrailColor
        {
            get => _trailColor;
            set
            {
                if (_trailColor != value)
                {
                    _trailColor = value;
                    _targetControl.Invalidate();
                }
            }
        }

        /// <summary>
        /// 轨迹宽度
        /// </summary>
        public int TrailWidth
        {
            get => _trailWidth;
            set => _trailWidth = Math.Max(1, Math.Max(10, value));
        }

        /// <summary>
        /// 轨迹透明度
        /// </summary>
        public float TrailOpacity
        {
            get => _trailOpacity;
            set => _trailOpacity = Math.Max(0f, Math.Min(1f, value));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加手势定义
        /// </summary>
        /// <param name="gesture">手势定义</param>
        public void AddGesture(GestureDefinition gesture)
        {
            if (gesture == null)
                throw new ArgumentNullException(nameof(gesture));

            lock (_lockObject)
            {
                _gestureDefinitions.Add(gesture);
            }
        }

        /// <summary>
        /// 移除手势定义
        /// </summary>
        /// <param name="gestureName">手势名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGesture(string gestureName)
        {
            if (string.IsNullOrEmpty(gestureName))
                return false;

            lock (_lockObject)
            {
                var gesture = _gestureDefinitions.FirstOrDefault(g => g.Name == gestureName);
                if (gesture != null)
                {
                    return _gestureDefinitions.Remove(gesture);
                }
                return false;
            }
        }

        /// <summary>
        /// 获取所有手势定义
        /// </summary>
        /// <returns>手势定义列表</returns>
        public IReadOnlyList<GestureDefinition> GetAllGestures()
        {
            lock (_lockObject)
            {
                return _gestureDefinitions.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// 开始录制手势
        /// </summary>
        /// <param name="startPoint">起始点</param>
        public void StartRecording(Point startPoint)
        {
            if (_isRecording)
                return;

            lock (_lockObject)
            {
                _isRecording = true;
                _recordingStartTime = DateTime.Now;
                _startPoint = startPoint;
                _currentPoint = startPoint;
                _gesturePoints.Clear();
                _gesturePoints.Add(startPoint);

                _recognitionTimer.Interval = _maxGestureTime;
                _recognitionTimer.Start();

                OnGestureStart(new GestureStartEventArgs(startPoint));
            }
        }

        /// <summary>
        /// 添加手势点
        /// </summary>
        /// <param name="point">手势点</param>
        public void AddGesturePoint(Point point)
        {
            if (!_isRecording)
                return;

            lock (_lockObject)
            {
                _currentPoint = point;
                _gesturePoints.Add(point);

                OnGestureProgress(new GestureProgressEventArgs(point, _gesturePoints.Count));

                if (_showGestureTrail)
                {
                    DrawTrailPoint(point);
                }
            }
        }

        /// <summary>
        /// 停止录制手势
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording)
                return;

            lock (_lockObject)
            {
                _isRecording = false;
                _recognitionTimer.Stop();

                var endPoint = _currentPoint;
                var duration = DateTime.Now - _recordingStartTime;

                OnGestureEnd(new GestureEndEventArgs(_startPoint, endPoint, _gesturePoints.ToList(), duration));

                // 尝试识别手势
                if (_gesturePoints.Count >= 2)
                {
                    RecognizeGesture();
                }

                // 延迟清除轨迹
                if (_showGestureTrail)
                {
                    var clearTimer = new Timer { Interval = 1000 };
                    clearTimer.Tick += (s, e) =>
                    {
                        clearTimer.Stop();
                        clearTimer.Dispose();
                        ClearTrail();
                        _targetControl.Invalidate();
                    };
                    clearTimer.Start();
                }
            }
        }

        /// <summary>
        /// 清除手势轨迹
        /// </summary>
        public void ClearTrail()
        {
            lock (_lockObject)
            {
                if (_trailGraphics != null)
                {
                    _trailGraphics.Clear(Color.Transparent);
                }
            }
        }

        /// <summary>
        /// 绘制手势轨迹
        /// </summary>
        /// <param name="g">绘图对象</param>
        /// <param name="offset">偏移量</param>
        public void DrawGestureTrail(Graphics g, Point offset)
        {
            if (!_showGestureTrail || _gesturePoints.Count < 2 || _trailBuffer == null)
                return;

            var adjustedOffset = new Point(offset.X, offset.Y);
            g.DrawImage(_trailBuffer, adjustedOffset);
        }

        /// <summary>
        /// 创建自定义手势
        /// </summary>
        /// <param name="name">手势名称</param>
        /// <param name="points">手势点集合</param>
        /// <param name="description">描述</param>
        /// <param name="action">手势动作</param>
        /// <returns>创建的手势定义</returns>
        public GestureDefinition CreateCustomGesture(string name, List<Point> points, string description, Action action)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Gesture name cannot be null or empty", nameof(name));
            if (points == null || points.Count < 2)
                throw new ArgumentException("Gesture points must contain at least 2 points", nameof(points));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var normalizedPoints = NormalizeGesturePoints(points);
            var features = ExtractGestureFeatures(normalizedPoints);

            var gesture = new GestureDefinition
            {
                Name = name,
                Description = description ?? "",
                TemplatePoints = normalizedPoints,
                Features = features,
                Action = action,
                GestureType = GestureType.Custom
            };

            return gesture;
        }

        /// <summary>
        /// 手势点匹配测试
        /// </summary>
        /// <param name="points">测试点集合</param>
        /// <param name="gestureName">目标手势名称</param>
        /// <returns>匹配分数（0-1）</returns>
        public float TestGestureMatching(List<Point> points, string gestureName)
        {
            if (points == null || points.Count < 2)
                return 0f;

            if (string.IsNullOrEmpty(gestureName))
                return 0f;

            lock (_lockObject)
            {
                var gesture = _gestureDefinitions.FirstOrDefault(g => g.Name == gestureName);
                if (gesture == null)
                    return 0f;

                var normalizedPoints = NormalizeGesturePoints(points);
                var features = ExtractGestureFeatures(normalizedPoints);

                return CalculateGestureSimilarity(features, gesture.Features);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化默认手势
        /// </summary>
        private void InitializeDefaultGestures()
        {
            // 水平向右滑动
            AddGesture(new GestureDefinition
            {
                Name = "SwipeRight",
                Description = "向右滑动",
                GestureType = GestureType.Swipe,
                TemplatePoints = GenerateLinePoints(new Point(0, 50), new Point(100, 50), 20),
                Action = () => OnGestureAction("SwipeRight")
            });

            // 水平向左滑动
            AddGesture(new GestureDefinition
            {
                Name = "SwipeLeft",
                Description = "向左滑动",
                GestureType = GestureType.Swipe,
                TemplatePoints = GenerateLinePoints(new Point(100, 50), new Point(0, 50), 20),
                Action = () => OnGestureAction("SwipeLeft")
            });

            // 垂直向上滑动
            AddGesture(new GestureDefinition
            {
                Name = "SwipeUp",
                Description = "向上滑动",
                GestureType = GestureType.Swipe,
                TemplatePoints = GenerateLinePoints(new Point(50, 100), new Point(50, 0), 20),
                Action = () => OnGestureAction("SwipeUp")
            });

            // 垂直向下滑动
            AddGesture(new GestureDefinition
            {
                Name = "SwipeDown",
                Description = "向下滑动",
                GestureType = GestureType.Swipe,
                TemplatePoints = GenerateLinePoints(new Point(50, 0), new Point(50, 100), 20),
                Action = () => OnGestureAction("SwipeDown")
            });

            // 圆形手势
            AddGesture(new GestureDefinition
            {
                Name = "Circle",
                Description = "圆形手势",
                GestureType = GestureType.Circle,
                TemplatePoints = GenerateCirclePoints(new Point(50, 50), 40, 30),
                Action = () => OnGestureAction("Circle")
            });

            // L形手势（右下）
            AddGesture(new GestureDefinition
            {
                Name = "LShapeRightDown",
                Description = "L形手势（右下）",
                GestureType = GestureType.LShape,
                TemplatePoints = GenerateLShapePoints(new Point(20, 20), new Point(80, 20), new Point(80, 80), 15),
                Action = () => OnGestureAction("LShapeRightDown")
            });

            // V形手势
            AddGesture(new GestureDefinition
            {
                Name = "VShape",
                Description = "V形手势",
                GestureType = GestureType.VShape,
                TemplatePoints = GenerateVShapePoints(new Point(20, 80), new Point(50, 20), new Point(80, 80), 15),
                Action = () => OnGestureAction("VShape")
            });

            // Z形手势
            AddGesture(new GestureDefinition
            {
                Name = "ZShape",
                Description = "Z形手势",
                GestureType = GestureType.ZShape,
                TemplatePoints = GenerateZShapePoints(new Point(20, 20), new Point(80, 20), new Point(20, 80), new Point(80, 80), 15),
                Action = () => OnGestureAction("ZShape")
            });

            // 提取所有手势的特征
            foreach (var gesture in _gestureDefinitions)
            {
                if (gesture.Features == null)
                {
                    gesture.Features = ExtractGestureFeatures(gesture.TemplatePoints);
                }
            }
        }

        /// <summary>
        /// 初始化识别定时器
        /// </summary>
        private void InitializeRecognitionTimer()
        {
            _recognitionTimer = new Timer();
            _recognitionTimer.Tick += OnRecognitionTimerTick;
        }

        /// <summary>
        /// 初始化可视化
        /// </summary>
        private void InitializeVisualization()
        {
            _showGestureTrail = true;
            InitializeTrailBuffer();
        }

        /// <summary>
        /// 初始化轨迹缓冲区
        /// </summary>
        private void InitializeTrailBuffer()
        {
            if (_targetControl != null)
            {
                _trailBuffer = new Bitmap(_targetControl.Width, _targetControl.Height);
                _trailGraphics = Graphics.FromImage(_trailBuffer);
                _trailGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            }
        }

        /// <summary>
        /// 附加事件处理程序
        /// </summary>
        private void AttachEvents()
        {
            _targetControl.MouseDown += OnTargetControlMouseDown;
            _targetControl.MouseMove += OnTargetControlMouseMove;
            _targetControl.MouseUp += OnTargetControlMouseUp;
            _targetControl.Resize += OnTargetControlResize;
        }

        /// <summary>
        /// 分离事件处理程序
        /// </summary>
        private void DetachEvents()
        {
            _targetControl.MouseDown -= OnTargetControlMouseDown;
            _targetControl.MouseMove -= OnTargetControlMouseMove;
            _targetControl.MouseUp -= OnTargetControlMouseUp;
            _targetControl.Resize -= OnTargetControlResize;
        }

        /// <summary>
        /// 识别手势
        /// </summary>
        private void RecognizeGesture()
        {
            if (_gesturePoints.Count < 2)
                return;

            try
            {
                // 简化手势点
                var simplifiedPoints = SimplifyGesturePoints(_gesturePoints);
                if (simplifiedPoints.Count < 2)
                    return;

                // 标准化手势点
                var normalizedPoints = NormalizeGesturePoints(simplifiedPoints);

                // 提取特征
                var features = ExtractGestureFeatures(normalizedPoints);

                // 匹配手势
                GestureDefinition bestMatch = null;
                float bestScore = 0f;

                lock (_lockObject)
                {
                    foreach (var gesture in _gestureDefinitions)
                    {
                        var score = CalculateGestureSimilarity(features, gesture.Features);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = gesture;
                        }
                    }
                }

                if (bestMatch != null && bestScore >= _recognitionThreshold)
                {
                    // 识别成功
                    OnGestureRecognized(new GestureRecognizedEventArgs(bestMatch, bestScore, _gesturePoints.ToList()));

                    try
                    {
                        bestMatch.Action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // 记录异常但不中断程序
                        System.Diagnostics.Debug.WriteLine($"手势动作执行异常: {ex.Message}");
                    }
                }
                else
                {
                    // 识别失败
                    OnGestureFailed(new GestureFailedEventArgs(_gesturePoints.ToList(), bestScore, bestMatch));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"手势识别异常: {ex.Message}");
                OnGestureFailed(new GestureFailedEventArgs(_gesturePoints.ToList(), 0f, null));
            }
        }

        /// <summary>
        /// 简化手势点
        /// </summary>
        private List<Point> SimplifyGesturePoints(List<Point> points)
        {
            if (points.Count <= 2)
                return new List<Point>(points);

            var simplified = new List<Point> { points[0] };

            for (int i = 1; i < points.Count - 1; i++)
            {
                var prev = simplified.Last();
                var current = points[i];
                var next = points[i + 1];

                // 检查是否可以忽略当前点
                var distance = DistanceFromPointToLine(current, prev, next);
                if (distance > _simplificationTolerance)
                {
                    simplified.Add(current);
                }
            }

            simplified.Add(points.Last());
            return simplified;
        }

        /// <summary>
        /// 标准化手势点
        /// </summary>
        private List<Point> NormalizeGesturePoints(List<Point> points)
        {
            if (points.Count < 2)
                return new List<Point>(points);

            // 计算边界框
            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);

            var width = maxX - minX;
            var height = maxY - minY;
            var scale = Math.Max(width, height);

            if (scale == 0)
                return new List<Point>(points);

            // 标准化到 0-100 范围
            var normalized = points.Select(p => new Point(
                (int)((p.X - minX) * 100 / scale),
                (int)((p.Y - minY) * 100 / scale)
            )).ToList();

            return normalized;
        }

        /// <summary>
        /// 提取手势特征
        /// </summary>
        private GestureFeatures ExtractGestureFeatures(List<Point> points)
        {
            if (points.Count < 2)
                return new GestureFeatures();

            var features = new GestureFeatures();

            // 基本统计特征
            features.PointCount = points.Count;
            features.TotalLength = CalculateTotalLength(points);
            features.AverageSegmentLength = features.TotalLength / (points.Count - 1);

            // 方向特征
            features.DirectionChanges = CountDirectionChanges(points);
            features.PrimaryDirection = GetPrimaryDirection(points);

            // 几何特征
            features.Bounds = CalculateBoundingBox(points);
            features.AspectRatio = features.Bounds.Width / (float)Math.Max(1, features.Bounds.Height);
            features.CenterOfMass = CalculateCenterOfMass(points);

            // 曲率特征
            features.AverageCurvature = CalculateAverageCurvature(points);
            features.MaxCurvature = CalculateMaxCurvature(points);

            // 特殊形状特征
            features.IsClosedShape = IsClosedShape(points);
            features.IsLinear = IsLinear(points);
            features.IsCircular = IsCircular(points);

            return features;
        }

        /// <summary>
        /// 计算手势相似度
        /// </summary>
        private float CalculateGestureSimilarity(GestureFeatures features1, GestureFeatures features2)
        {
            if (features1 == null || features2 == null)
                return 0f;

            float totalScore = 0f;
            int featureCount = 0;

            // 点数量相似度
            var pointScore = 1f - Math.Abs(features1.PointCount - features2.PointCount) / (float)Math.Max(features1.PointCount, features2.PointCount);
            totalScore += pointScore * 0.1f;
            featureCount++;

            // 长度相似度
            var lengthScore = 1f - Math.Abs(features1.TotalLength - features2.TotalLength) / Math.Max(features1.TotalLength, features2.TotalLength);
            totalScore += lengthScore * 0.1f;
            featureCount++;

            // 方向变化相似度
            var directionScore = 1f - Math.Abs(features1.DirectionChanges - features2.DirectionChanges) / (float)Math.Max(features1.DirectionChanges, features2.DirectionChanges);
            totalScore += directionScore * 0.15f;
            featureCount++;

            // 长宽比相似度
            var aspectRatioScore = 1f - Math.Abs(features1.AspectRatio - features2.AspectRatio) / Math.Max(features1.AspectRatio, features2.AspectRatio);
            totalScore += aspectRatioScore * 0.15f;
            featureCount++;

            // 封闭形状相似度
            var closedScore = features1.IsClosedShape == features2.IsClosedShape ? 1f : 0f;
            totalScore += closedScore * 0.2f;
            featureCount++;

            // 线性相似度
            var linearScore = features1.IsLinear == features2.IsLinear ? 1f : 0f;
            totalScore += linearScore * 0.15f;
            featureCount++;

            // 圆形相似度
            var circularScore = features1.IsCircular == features2.IsCircular ? 1f : 0f;
            totalScore += circularScore * 0.15f;
            featureCount++;

            return featureCount > 0 ? totalScore / featureCount : 0f;
        }

        /// <summary>
        /// 绘制轨迹点
        /// </summary>
        private void DrawTrailPoint(Point point)
        {
            if (_trailGraphics == null)
                return;

            using (var pen = new Pen(Color.FromArgb((int)(_trailOpacity * 255), _trailColor), _trailWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                if (_gesturePoints.Count > 1)
                {
                    var prevPoint = _gesturePoints[_gesturePoints.Count - 2];
                    _trailGraphics.DrawLine(pen, prevPoint, point);
                }
            }
        }

        #endregion

        #region 辅助方法

        private double DistanceFromPointToLine(Point point, Point lineStart, Point lineEnd)
        {
            var A = point.X - lineStart.X;
            var B = point.Y - lineStart.Y;
            var C = lineEnd.X - lineStart.X;
            var D = lineEnd.Y - lineStart.Y;

            var dot = A * C + B * D;
            var lenSq = C * C + D * D;
            if (lenSq == 0)
                return Math.Sqrt(A * A + B * B);

            var param = dot / lenSq;

            int xx, yy;

            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + (int)(param * C);
                yy = lineStart.Y + (int)(param * D);
            }

            var dx = point.X - xx;
            var dy = point.Y - yy;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double CalculateTotalLength(List<Point> points)
        {
            double totalLength = 0;
            for (int i = 1; i < points.Count; i++)
            {
                var dx = points[i].X - points[i - 1].X;
                var dy = points[i].Y - points[i - 1].Y;
                totalLength += Math.Sqrt(dx * dx + dy * dy);
            }
            return totalLength;
        }

        private int CountDirectionChanges(List<Point> points)
        {
            if (points.Count < 3)
                return 0;

            int changes = 0;
            Direction? prevDirection = null;

            for (int i = 1; i < points.Count; i++)
            {
                var dx = points[i].X - points[i - 1].X;
                var dy = points[i].Y - points[i - 1].Y;

                Direction currentDirection;
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    currentDirection = dx > 0 ? Direction.Right : Direction.Left;
                }
                else
                {
                    currentDirection = dy > 0 ? Direction.Down : Direction.Up;
                }

                if (prevDirection.HasValue && prevDirection.Value != currentDirection)
                {
                    changes++;
                }

                prevDirection = currentDirection;
            }

            return changes;
        }

        private Direction GetPrimaryDirection(List<Point> points)
        {
            if (points.Count < 2)
                return Direction.None;

            var first = points.First();
            var last = points.Last();
            var dx = last.X - first.X;
            var dy = last.Y - first.Y;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                return dy > 0 ? Direction.Down : Direction.Up;
            }
        }

        private Rectangle CalculateBoundingBox(List<Point> points)
        {
            if (points.Count == 0)
                return Rectangle.Empty;

            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private PointF CalculateCenterOfMass(List<Point> points)
        {
            if (points.Count == 0)
                return PointF.Empty;

            var sumX = points.Sum(p => p.X);
            var sumY = points.Sum(p => p.Y);

            return new PointF(sumX / (float)points.Count, sumY / (float)points.Count);
        }

        private float CalculateAverageCurvature(List<Point> points)
        {
            if (points.Count < 3)
                return 0f;

            float totalCurvature = 0;
            int count = 0;

            for (int i = 1; i < points.Count - 1; i++)
            {
                var p1 = points[i - 1];
                var p2 = points[i];
                var p3 = points[i + 1];

                // 计算三点形成的角度
                var v1 = new PointF(p1.X - p2.X, p1.Y - p2.Y);
                var v2 = new PointF(p3.X - p2.X, p3.Y - p2.Y);

                var dot = v1.X * v2.X + v1.Y * v2.Y;
                var len1 = (float)Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
                var len2 = (float)Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);

                if (len1 > 0 && len2 > 0)
                {
                    var cosAngle = dot / (len1 * len2);
                    cosAngle = Math.Max(-1, Math.Min(1, cosAngle));
                    var angle = (float)Math.Acos(cosAngle);
                    totalCurvature += angle;
                    count++;
                }
            }

            return count > 0 ? totalCurvature / count : 0f;
        }

        private float CalculateMaxCurvature(List<Point> points)
        {
            if (points.Count < 3)
                return 0f;

            float maxCurvature = 0f;

            for (int i = 1; i < points.Count - 1; i++)
            {
                var p1 = points[i - 1];
                var p2 = points[i];
                var p3 = points[i + 1];

                var v1 = new PointF(p1.X - p2.X, p1.Y - p2.Y);
                var v2 = new PointF(p3.X - p2.X, p3.Y - p2.Y);

                var dot = v1.X * v2.X + v1.Y * v2.Y;
                var len1 = (float)Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
                var len2 = (float)Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);

                if (len1 > 0 && len2 > 0)
                {
                    var cosAngle = dot / (len1 * len2);
                    cosAngle = Math.Max(-1, Math.Min(1, cosAngle));
                    var angle = (float)Math.Acos(cosAngle);
                    maxCurvature = Math.Max(maxCurvature, angle);
                }
            }

            return maxCurvature;
        }

        private bool IsClosedShape(List<Point> points)
        {
            if (points.Count < 3)
                return false;

            var first = points.First();
            var last = points.Last();
            var distance = Math.Sqrt(Math.Pow(last.X - first.X, 2) + Math.Pow(last.Y - first.Y, 2));
            var threshold = CalculateTotalLength(points) * 0.1; // 10% of total length

            return distance <= threshold;
        }

        private bool IsLinear(List<Point> points)
        {
            if (points.Count < 3)
                return true;

            var first = points.First();
            var last = points.Last();

            double totalDeviation = 0;
            foreach (var point in points.Skip(1).Take(points.Count - 2))
            {
                var deviation = DistanceFromPointToLine(point, first, last);
                totalDeviation += deviation;
            }

            var averageDeviation = totalDeviation / (points.Count - 2);
            return averageDeviation < _simplificationTolerance;
        }

        private bool IsCircular(List<Point> points)
        {
            if (points.Count < 5)
                return false;

            // 简单的圆形检测：检查点到中心的距离变化
            var center = CalculateCenterOfMass(points);
            var distances = points.Select(p => Math.Sqrt(Math.Pow(p.X - center.X, 2) + Math.Pow(p.Y - center.Y, 2))).ToList();

            var avgDistance = distances.Average();
            var variance = distances.Select(d => Math.Pow(d - avgDistance, 2)).Average();
            var stdDeviation = Math.Sqrt(variance);

            // 如果标准差小于平均距离的20%，认为是圆形
            return stdDeviation < avgDistance * 0.2;
        }

        // 手势生成方法
        private List<Point> GenerateLinePoints(Point start, Point end, int pointCount)
        {
            var points = new List<Point>();
            for (int i = 0; i < pointCount; i++)
            {
                var t = (float)i / (pointCount - 1);
                var x = start.X + (int)((end.X - start.X) * t);
                var y = start.Y + (int)((end.Y - start.Y) * t);
                points.Add(new Point(x, y));
            }
            return points;
        }

        private List<Point> GenerateCirclePoints(Point center, int radius, int pointCount)
        {
            var points = new List<Point>();
            for (int i = 0; i < pointCount; i++)
            {
                var angle = 2 * Math.PI * i / pointCount;
                var x = center.X + (int)(radius * Math.Cos(angle));
                var y = center.Y + (int)(radius * Math.Sin(angle));
                points.Add(new Point(x, y));
            }
            return points;
        }

        private List<Point> GenerateLShapePoints(Point start, Point corner, Point end, int segmentsPerSide)
        {
            var points = new List<Point>();

            // 第一条边
            for (int i = 0; i < segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = start.X + (int)((corner.X - start.X) * t);
                var y = start.Y + (int)((corner.Y - start.Y) * t);
                points.Add(new Point(x, y));
            }

            // 第二条边
            for (int i = 0; i <= segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = corner.X + (int)((end.X - corner.X) * t);
                var y = corner.Y + (int)((end.Y - corner.Y) * t);
                points.Add(new Point(x, y));
            }

            return points;
        }

        private List<Point> GenerateVShapePoints(Point start, Point bottom, Point end, int segmentsPerSide)
        {
            var points = new List<Point>();

            // 左边
            for (int i = 0; i <= segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = start.X + (int)((bottom.X - start.X) * t);
                var y = start.Y + (int)((bottom.Y - start.Y) * t);
                points.Add(new Point(x, y));
            }

            // 右边
            for (int i = 1; i <= segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = bottom.X + (int)((end.X - bottom.X) * t);
                var y = bottom.Y + (int)((end.Y - bottom.Y) * t);
                points.Add(new Point(x, y));
            }

            return points;
        }

        private List<Point> GenerateZShapePoints(Point p1, Point p2, Point p3, Point p4, int segmentsPerSide)
        {
            var points = new List<Point>();

            // 第一条横线
            for (int i = 0; i < segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = p1.X + (int)((p2.X - p1.X) * t);
                var y = p1.Y + (int)((p2.Y - p1.Y) * t);
                points.Add(new Point(x, y));
            }

            // 对角线
            for (int i = 0; i <= segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = p2.X + (int)((p3.X - p2.X) * t);
                var y = p2.Y + (int)((p3.Y - p2.Y) * t);
                points.Add(new Point(x, y));
            }

            // 第二条横线
            for (int i = 1; i <= segmentsPerSide; i++)
            {
                var t = (float)i / segmentsPerSide;
                var x = p3.X + (int)((p4.X - p3.X) * t);
                var y = p3.Y + (int)((p4.Y - p3.Y) * t);
                points.Add(new Point(x, y));
            }

            return points;
        }

        #endregion

        #region 事件处理程序

        private void OnTargetControlMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                StartRecording(e.Location);
            }
        }

        private void OnTargetControlMouseMove(object sender, MouseEventArgs e)
        {
            if (_isRecording)
            {
                AddGesturePoint(e.Location);
            }
        }

        private void OnTargetControlMouseUp(object sender, MouseEventArgs e)
        {
            if (_isRecording && e.Button == MouseButtons.Right)
            {
                StopRecording();
            }
        }

        private void OnTargetControlResize(object sender, EventArgs e)
        {
            // 重新初始化轨迹缓冲区
            var oldBuffer = _trailBuffer;
            InitializeTrailBuffer();
            oldBuffer?.Dispose();
        }

        private void OnRecognitionTimerTick(object sender, EventArgs e)
        {
            _recognitionTimer.Stop();
            if (_isRecording)
            {
                StopRecording();
            }
        }

        #endregion

        #region 事件触发方法

        protected virtual void OnGestureStart(GestureStartEventArgs e)
        {
            GestureStart?.Invoke(this, e);
        }

        protected virtual void OnGestureProgress(GestureProgressEventArgs e)
        {
            GestureProgress?.Invoke(this, e);
        }

        protected virtual void OnGestureEnd(GestureEndEventArgs e)
        {
            GestureEnd?.Invoke(this, e);
        }

        protected virtual void OnGestureRecognized(GestureRecognizedEventArgs e)
        {
            GestureRecognized?.Invoke(this, e);
        }

        protected virtual void OnGestureFailed(GestureFailedEventArgs e)
        {
            GestureFailed?.Invoke(this, e);
        }

        protected virtual void OnGestureAction(string gestureName)
        {
            // 可以在这里添加默认的手势动作处理
            System.Diagnostics.Debug.WriteLine($"手势动作触发: {gestureName}");
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

                _recognitionTimer?.Stop();
                _recognitionTimer?.Dispose();

                _trailGraphics?.Dispose();
                _trailBuffer?.Dispose();

                _disposed = true;
            }
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 手势定义
    /// </summary>
    public class GestureDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public GestureType GestureType { get; set; }
        public List<Point> TemplatePoints { get; set; }
        public GestureFeatures Features { get; set; }
        public Action Action { get; set; }
    }

    /// <summary>
    /// 手势特征
    /// </summary>
    public class GestureFeatures
    {
        public int PointCount { get; set; }
        public double TotalLength { get; set; }
        public double AverageSegmentLength { get; set; }
        public int DirectionChanges { get; set; }
        public Direction PrimaryDirection { get; set; }
        public Rectangle Bounds { get; set; }
        public float AspectRatio { get; set; }
        public PointF CenterOfMass { get; set; }
        public float AverageCurvature { get; set; }
        public float MaxCurvature { get; set; }
        public bool IsClosedShape { get; set; }
        public bool IsLinear { get; set; }
        public bool IsCircular { get; set; }
    }

    /// <summary>
    /// 手势类型
    /// </summary>
    public enum GestureType
    {
        Swipe,
        Circle,
        LShape,
        VShape,
        ZShape,
        Triangle,
        Square,
        Star,
        Custom
    }

    /// <summary>
    /// 方向枚举
    /// </summary>
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// 手势开始事件参数
    /// </summary>
    public class GestureStartEventArgs : EventArgs
    {
        public Point StartPosition { get; }
        public DateTime StartTime { get; }

        public GestureStartEventArgs(Point startPosition)
        {
            StartPosition = startPosition;
            StartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 手势进行中事件参数
    /// </summary>
    public class GestureProgressEventArgs : EventArgs
    {
        public Point CurrentPosition { get; }
        public int PointCount { get; }
        public DateTime Timestamp { get; }

        public GestureProgressEventArgs(Point currentPosition, int pointCount)
        {
            CurrentPosition = currentPosition;
            PointCount = pointCount;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 手势结束事件参数
    /// </summary>
    public class GestureEndEventArgs : EventArgs
    {
        public Point StartPosition { get; }
        public Point EndPosition { get; }
        public List<Point> AllPoints { get; }
        public TimeSpan Duration { get; }
        public DateTime EndTime { get; }

        public GestureEndEventArgs(Point startPosition, Point endPosition, List<Point> allPoints, TimeSpan duration)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            AllPoints = allPoints ?? new List<Point>();
            Duration = duration;
            EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 手势识别成功事件参数
    /// </summary>
    public class GestureRecognizedEventArgs : EventArgs
    {
        public GestureDefinition Gesture { get; }
        public float ConfidenceScore { get; }
        public List<Point> InputPoints { get; }
        public DateTime RecognizedAt { get; }

        public GestureRecognizedEventArgs(GestureDefinition gesture, float confidenceScore, List<Point> inputPoints)
        {
            Gesture = gesture;
            ConfidenceScore = confidenceScore;
            InputPoints = inputPoints ?? new List<Point>();
            RecognizedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 手势识别失败事件参数
    /// </summary>
    public class GestureFailedEventArgs : EventArgs
    {
        public List<Point> InputPoints { get; }
        public float BestScore { get; }
        public GestureDefinition BestMatch { get; }
        public DateTime FailedAt { get; }
        public string Reason { get; }

        public GestureFailedEventArgs(List<Point> inputPoints, float bestScore, GestureDefinition bestMatch, string reason = null)
        {
            InputPoints = inputPoints ?? new List<Point>();
            BestScore = bestScore;
            BestMatch = bestMatch;
            FailedAt = DateTime.Now;
            Reason = reason ?? (bestScore < 0.5f ? "匹配度过低" : "未找到匹配的手势");
        }
    }

    #endregion
}