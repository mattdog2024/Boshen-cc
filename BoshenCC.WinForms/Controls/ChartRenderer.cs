using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using BoshenCC.Core.Utils;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 图表渲染器控件
    /// 支持多种渲染模式、动态效果和专业的数据可视化功能
    /// </summary>
    public class ChartRenderer : Control
    {
        #region 事件

        /// <summary>
        /// 渲染开始事件
        /// </summary>
        public event EventHandler<RenderStartEventArgs> RenderStart;

        /// <summary>
        /// 渲染完成事件
        /// </summary>
        public event EventHandler<RenderCompleteEventArgs> RenderComplete;

        /// <summary>
        /// 渲染模式改变事件
        /// </summary>
        public event EventHandler<RenderModeChangedEventArgs> RenderModeChanged;

        /// <summary>
        /// 数据点悬停事件
        /// </summary>
        public event EventHandler<DataPointHoverEventArgs> DataPointHover;

        /// <summary>
        /// 数据点点击事件
        /// </summary>
        public event EventHandler<DataPointClickEventArgs> DataPointClick;

        #endregion

        #region 私有字段

        private ChartData _chartData;
        private RenderMode _renderMode = RenderMode.Standard;
        private ChartTheme _theme = ChartTheme.Default;
        private Rectangle _chartArea;
        private Rectangle _legendArea;
        private Rectangle _titleArea;
        private ChartDataPoint _hoveredPoint;
        private ChartDataPoint _selectedPoint;
        private Bitmap _backBuffer;
        private bool _needsRedraw;

        // 渲染设置
        private bool _showGrid = true;
        private bool _showLegend = true;
        private bool _showTitle = true;
        private bool _showTooltip = true;
        private bool _enableAnimation = true;
        private int _animationDuration = 1000; // 毫秒
        private double _animationProgress = 1.0;

        // 缩放和平移
        private float _zoomFactor = 1.0f;
        private PointF _panOffset = PointF.Empty;
        private bool _isPanning;
        private Point _lastPanPoint;

        // 交互状态
        private Point _mousePosition;
        private ToolTip _toolTip;

        // 渲染缓存
        private readonly Dictionary<string, Bitmap> _seriesCache;
        private readonly Dictionary<string, RenderPath> _pathCache;

        // 性能监控
        private DateTime _renderStartTime;
        private TimeSpan _lastRenderTime;

        #endregion

        #region 枚举

        /// <summary>
        /// 渲染模式
        /// </summary>
        public enum RenderMode
        {
            /// <summary>
            /// 标准模式
            /// </summary>
            Standard,

            /// <summary>
            /// 3D模式
            /// </summary>
            ThreeDimensional,

            /// <summary>
            /// 渐变模式
            /// </summary>
            Gradient,

            /// <summary>
            /// 阴影模式
            /// </summary>
            Shadow,

            /// <summary>
            /// 发光模式
            /// </summary>
            Glow,

            /// <summary>
            /// 玻璃模式
            /// </summary>
            Glass
        }

        /// <summary>
        /// 图表主题
        /// </summary>
        public enum ChartTheme
        {
            /// <summary>
            /// 默认主题
            /// </summary>
            Default,

            /// <summary>
            /// 深色主题
            /// </summary>
            Dark,

            /// <summary>
            /// 亮色主题
            /// </summary>
            Light,

            /// <summary>
            /// 专业主题
            /// </summary>
            Professional,

            /// <summary>
            /// 彩色主题
            /// </summary>
            Colorful
        }

        /// <summary>
        /// 图表类型
        /// </summary>
        public enum ChartType
        {
            /// <summary>
            /// 线形图
            /// </summary>
            Line,

            /// <summary>
            /// 柱状图
            /// </summary>
            Bar,

            /// <summary>
            /// 面积图
            /// </summary>
            Area,

            /// <summary>
            /// 散点图
            /// </summary>
            Scatter,

            /// <summary>
            /// 雷达图
            /// </summary>
            Radar,

            /// <summary>
            /// 饼图
            /// </summary>
            Pie
        }

        #endregion

        #region 构造函数

        public ChartRenderer()
        {
            InitializeComponent();

            _chartData = new ChartData();
            _seriesCache = new Dictionary<string, Bitmap>();
            _pathCache = new Dictionary<string, RenderPath>();
            _toolTip = new ToolTip();

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 图表数据
        /// </summary>
        public ChartData ChartData
        {
            get => _chartData;
            set
            {
                if (_chartData != value)
                {
                    _chartData = value ?? new ChartData();
                    InvalidateLayout();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 渲染模式
        /// </summary>
        public RenderMode CurrentRenderMode
        {
            get => _renderMode;
            set
            {
                if (_renderMode != value)
                {
                    var oldMode = _renderMode;
                    _renderMode = value;
                    InvalidateCache();
                    OnRenderModeChanged(new RenderModeChangedEventArgs(oldMode, _renderMode));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 图表主题
        /// </summary>
        public ChartTheme CurrentTheme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    InvalidateCache();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 图表区域
        /// </summary>
        public Rectangle ChartArea => _chartArea;

        /// <summary>
        /// 图例区域
        /// </summary>
        public Rectangle LegendArea => _legendArea;

        /// <summary>
        /// 标题区域
        /// </summary>
        public Rectangle TitleArea => _titleArea;

        /// <summary>
        /// 是否显示网格
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (_showGrid != value)
                {
                    _showGrid = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否显示图例
        /// </summary>
        public bool ShowLegend
        {
            get => _showLegend;
            set
            {
                if (_showLegend != value)
                {
                    _showLegend = value;
                    InvalidateLayout();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否显示标题
        /// </summary>
        public bool ShowTitle
        {
            get => _showTitle;
            set
            {
                if (_showTitle != value)
                {
                    _showTitle = value;
                    InvalidateLayout();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool EnableAnimation
        {
            get => _enableAnimation;
            set => _enableAnimation = value;
        }

        /// <summary>
        /// 动画持续时间（毫秒）
        /// </summary>
        public int AnimationDuration
        {
            get => _animationDuration;
            set => _animationDuration = Math.Max(100, value);
        }

        /// <summary>
        /// 缩放因子
        /// </summary>
        public float ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                if (Math.Abs(_zoomFactor - value) > 0.001f)
                {
                    _zoomFactor = Math.Max(0.1f, Math.Min(10.0f, value));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 平移偏移
        /// </summary>
        public PointF PanOffset
        {
            get => _panOffset;
            set
            {
                if (_panOffset != value)
                {
                    _panOffset = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 当前悬停的数据点
        /// </summary>
        public ChartDataPoint HoveredPoint
        {
            get => _hoveredPoint;
            private set
            {
                if (_hoveredPoint != value)
                {
                    _hoveredPoint = value;
                    OnDataPointHover(new DataPointHoverEventArgs(_hoveredPoint));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 当前选中的数据点
        /// </summary>
        public ChartDataPoint SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (_selectedPoint != value)
                {
                    _selectedPoint = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 最后渲染时间
        /// </summary>
        public TimeSpan LastRenderTime => _lastRenderTime;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置图表数据并开始动画
        /// </summary>
        /// <param name="data">图表数据</param>
        public void SetChartData(ChartData data)
        {
            ChartData = data;

            if (EnableAnimation && data != null && data.Series.Count > 0)
            {
                StartAnimation();
            }
        }

        /// <summary>
        /// 添加数据系列
        /// </summary>
        /// <param name="series">数据系列</param>
        public void AddSeries(ChartSeries series)
        {
            if (series != null)
            {
                _chartData.Series.Add(series);
                InvalidateLayout();
                Invalidate();
            }
        }

        /// <summary>
        /// 移除数据系列
        /// </summary>
        /// <param name="seriesName">系列名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveSeries(string seriesName)
        {
            var series = _chartData.Series.FirstOrDefault(s => s.Name == seriesName);
            if (series != null)
            {
                _chartData.Series.Remove(series);
                InvalidateLayout();
                Invalidate();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearData()
        {
            _chartData.Series.Clear();
            _hoveredPoint = null;
            _selectedPoint = null;
            InvalidateLayout();
            Invalidate();
        }

        /// <summary>
        /// 开始动画
        /// </summary>
        public void StartAnimation()
        {
            if (!EnableAnimation)
                return;

            _animationProgress = 0.0;
            _renderStartTime = DateTime.Now;

            var timer = new Timer();
            timer.Interval = 16; // ~60 FPS
            timer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _renderStartTime;
                _animationProgress = Math.Min(1.0, elapsed.TotalMilliseconds / _animationDuration);

                Invalidate();

                if (_animationProgress >= 1.0)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        /// <summary>
        /// 适应视图到数据
        /// </summary>
        public void FitToData()
        {
            if (_chartData.Series.Count == 0)
                return;

            var bounds = CalculateDataBounds();
            if (!bounds.IsEmpty)
            {
                ZoomFactor = 1.0f;
                PanOffset = PointF.Empty;
            }
        }

        /// <summary>
        /// 缩放到指定区域
        /// </summary>
        /// <param name="rect">缩放区域</param>
        public void ZoomToRectangle(RectangleF rect)
        {
            if (rect.IsEmpty)
                return;

            var scaleX = _chartArea.Width / rect.Width;
            var scaleY = _chartArea.Height / rect.Height;
            var newZoom = Math.Min(scaleX, scaleY);

            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;

            ZoomFactor = newZoom;
            PanOffset = new PointF(
                _chartArea.X + _chartArea.Width / 2 - centerX * newZoom,
                _chartArea.Y + _chartArea.Height / 2 - centerY * newZoom
            );
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        public void ResetView()
        {
            ZoomFactor = 1.0f;
            PanOffset = PointF.Empty;
        }

        /// <summary>
        /// 导出图表为图像
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="format">图像格式</param>
        /// <returns>图像</returns>
        public Bitmap ExportToImage(int width, int height, ImageFormat format = null)
        {
            format = format ?? ImageFormat.Png;
            var bitmap = new Bitmap(width, height);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 临时调整布局以适应导出尺寸
            var originalSize = Size;
            var originalChartArea = _chartArea;
            var originalLegendArea = _legendArea;
            var originalTitleArea = _titleArea;

            Size = new Size(width, height);
            CalculateLayout();

            // 渲染到图像
            RenderChart(graphics);

            // 恢复原始布局
            Size = originalSize;
            _chartArea = originalChartArea;
            _legendArea = originalLegendArea;
            _titleArea = originalTitleArea;

            return bitmap;
        }

        /// <summary>
        /// 获取指定位置的数据点
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>数据点，如果没有找到返回null</returns>
        public ChartDataPoint GetDataPointAt(int x, int y)
        {
            var chartPoint = ScreenToChart(new Point(x, y));

            foreach (var series in _chartData.Series)
            {
                foreach (var point in series.Points)
                {
                    var screenPoint = ChartToScreen(new PointF(point.X, point.Y));
                    var distance = Math.Sqrt(Math.Pow(x - screenPoint.X, 2) + Math.Pow(y - screenPoint.Y, 2));

                    if (distance <= 5) // 5像素的容差
                    {
                        return point;
                    }
                }
            }

            return null;
        }

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var startTime = DateTime.Now;

            if (_backBuffer == null || _backBuffer.Size != Size)
            {
                CreateBackBuffer();
            }

            if (_needsRedraw)
            {
                DrawToBackBuffer();
                _needsRedraw = false;
            }

            // 绘制后备缓冲区内容
            e.Graphics.DrawImageUnscaled(_backBuffer, 0, 0);

            _lastRenderTime = DateTime.Now - startTime;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            InvalidateLayout();
            CreateBackBuffer();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            _mousePosition = e.Location;

            if (_isPanning)
            {
                var delta = new PointF(
                    e.X - _lastPanPoint.X,
                    e.Y - _lastPanPoint.Y
                );
                PanOffset = new PointF(
                    _panOffset.X + delta.X,
                    _panOffset.Y + delta.Y
                );
                _lastPanPoint = e.Location;
            }
            else
            {
                var previousHoveredPoint = HoveredPoint;
                HoveredPoint = GetDataPointAt(e.X, e.Y);

                // 更新工具提示
                if (HoveredPoint != null && _showTooltip)
                {
                    var tooltipText = $"{HoveredPoint.Series.Name}: {HoveredPoint.Label} = {HoveredPoint.Value:F2}";
                    _toolTip.Show(tooltipText, this, e.X + 10, e.Y - 25);
                }
                else
                {
                    _toolTip.Hide(this);
                }

                // 更新鼠标光标
                Cursor = HoveredPoint != null ? Cursors.Hand : Cursors.Default;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                _lastPanPoint = e.Location;
                Cursor = Cursors.Hand;
            }
            else if (e.Button == MouseButtons.Left)
            {
                var clickedPoint = GetDataPointAt(e.X, e.Y);
                if (clickedPoint != null)
                {
                    SelectedPoint = clickedPoint;
                    OnDataPointClick(new DataPointClickEventArgs(clickedPoint, e.Location));
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Middle && _isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredPoint = null;
            _toolTip.Hide(this);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Control.ModifierKeys == Keys.Control)
            {
                var scaleFactor = e.Delta > 0 ? 1.1f : 0.9f;
                ZoomFactor *= scaleFactor;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backBuffer?.Dispose();
                _toolTip?.Dispose();
                ClearCache();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region 私有方法

        private void InitializeComponent()
        {
            BackColor = Color.White;
            ResizeRedraw = true;
        }

        private void InvalidateLayout()
        {
            CalculateLayout();
            InvalidateCache();
        }

        private void CalculateLayout()
        {
            var padding = 20;
            var titleHeight = ShowTitle && !string.IsNullOrEmpty(_chartData.Title) ? 40 : 0;
            var legendWidth = ShowLegend ? 120 : 0;

            _titleArea = new Rectangle(
                padding,
                padding,
                Width - padding * 2 - legendWidth,
                titleHeight
            );

            _legendArea = ShowLegend ? new Rectangle(
                Width - legendWidth - padding,
                padding + titleHeight,
                legendWidth,
                Height - padding * 2 - titleHeight
            ) : Rectangle.Empty;

            _chartArea = new Rectangle(
                padding,
                padding + titleHeight,
                Width - padding * 2 - legendWidth,
                Height - padding * 2 - titleHeight
            );
        }

        private void CreateBackBuffer()
        {
            _backBuffer?.Dispose();
            _backBuffer = new Bitmap(Width, Height);
            _needsRedraw = true;
        }

        private void DrawToBackBuffer()
        {
            using var graphics = Graphics.FromImage(_backBuffer);

            // 设置高质量渲染
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 清除背景
            graphics.Clear(GetThemeColor(ThemeColor.BackgroundColor));

            // 触发渲染开始事件
            OnRenderStart(new RenderStartEventArgs());

            // 渲染各个组件
            if (ShowTitle && !string.IsNullOrEmpty(_chartData.Title))
            {
                RenderTitle(graphics);
            }

            RenderChart(graphics);

            if (ShowLegend)
            {
                RenderLegend(graphics);
            }

            // 触发渲染完成事件
            OnRenderComplete(new RenderCompleteEventArgs(_lastRenderTime));
        }

        private void RenderChart(Graphics graphics)
        {
            if (_chartData.Series.Count == 0)
                return;

            graphics.Save();

            // 应用变换
            graphics.TranslateTransform(_panOffset.X, _panOffset.Y);
            graphics.ScaleTransform(_zoomFactor, _zoomFactor);

            // 绘制网格
            if (_showGrid)
            {
                RenderGrid(graphics);
            }

            // 绘制坐标轴
            RenderAxes(graphics);

            // 根据渲染模式绘制数据
            switch (_renderMode)
            {
                case RenderMode.ThreeDimensional:
                    Render3DChart(graphics);
                    break;
                case RenderMode.Gradient:
                    RenderGradientChart(graphics);
                    break;
                case RenderMode.Shadow:
                    RenderShadowChart(graphics);
                    break;
                case RenderMode.Glow:
                    RenderGlowChart(graphics);
                    break;
                case RenderMode.Glass:
                    RenderGlassChart(graphics);
                    break;
                default:
                    RenderStandardChart(graphics);
                    break;
            }

            graphics.Restore();
        }

        private void RenderStandardChart(Graphics graphics)
        {
            foreach (var series in _chartData.Series)
            {
                RenderSeries(graphics, series);
            }
        }

        private void Render3DChart(Graphics graphics)
        {
            const float depth = 20f;

            foreach (var series in _chartData.Series)
            {
                // 绘制阴影（深度效果）
                graphics.Save();
                graphics.TranslateTransform(depth, depth);
                graphics.ScaleTransform(0.9f, 0.9f);

                using var shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Gray));
                RenderSeriesCore(graphics, series, shadowBrush, null);
                graphics.Restore();

                // 绘制主体
                RenderSeries(graphics, series);
            }
        }

        private void RenderGradientChart(Graphics graphics)
        {
            foreach (var series in _chartData.Series)
            {
                using var brush = new LinearGradientBrush(
                    _chartArea,
                    series.Color,
                    Color.FromArgb(100, series.Color),
                    LinearGradientMode.Vertical
                );

                RenderSeriesCore(graphics, series, brush, null);
            }
        }

        private void RenderShadowChart(Graphics graphics)
        {
            const int shadowOffset = 5;

            foreach (var series in _chartData.Series)
            {
                // 绘制阴影
                graphics.Save();
                graphics.TranslateTransform(shadowOffset, shadowOffset);

                using var shadowBrush = new SolidBrush(Color.FromArgb(80, Color.Black));
                RenderSeriesCore(graphics, series, shadowBrush, null);
                graphics.Restore();

                // 绘制主体
                RenderSeries(graphics, series);
            }
        }

        private void RenderGlowChart(Graphics graphics)
        {
            foreach (var series in _chartData.Series)
            {
                // 绘制发光效果
                for (int i = 3; i > 0; i--)
                {
                    graphics.Save();
                    graphics.TranslateTransform(0, 0);

                    var glowColor = Color.FromArgb(30 * i, series.Color);
                    using var glowPen = new Pen(glowColor, i * 2);
                    using var glowBrush = new SolidBrush(glowColor);

                    RenderSeriesCore(graphics, series, glowBrush, glowPen);
                    graphics.Restore();
                }

                // 绘制主体
                RenderSeries(graphics, series);
            }
        }

        private void RenderGlassChart(Graphics graphics)
        {
            foreach (var series in _chartData.Series)
            {
                // 绘制背景
                RenderSeries(graphics, series);

                // 绘制玻璃效果
                using var glassBrush = new LinearGradientBrush(
                    _chartArea,
                    Color.FromArgb(100, Color.White),
                    Color.FromArgb(20, Color.White),
                    LinearGradientMode.Vertical
                );

                RenderSeriesCore(graphics, series, glassBrush, null);
            }
        }

        private void RenderSeries(Graphics graphics, ChartSeries series)
        {
            using var brush = new SolidBrush(series.Color);
            using var pen = new Pen(series.Color, 2);

            RenderSeriesCore(graphics, series, brush, pen);
        }

        private void RenderSeriesCore(Graphics graphics, ChartSeries series, Brush brush, Pen pen)
        {
            if (series.Points.Count == 0)
                return;

            var points = series.Points
                .Select(p => ChartToScreen(new PointF(p.X, p.Y)))
                .ToArray();

            // 应用动画进度
            if (_animationProgress < 1.0)
            {
                var centerY = _chartArea.Y + _chartArea.Height / 2;
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new PointF(
                        points[i].X,
                        centerY + (points[i].Y - centerY) * (float)_animationProgress
                    );
                }
            }

            switch (series.Type)
            {
                case ChartType.Line:
                    RenderLineSeries(graphics, points, pen);
                    break;
                case ChartType.Bar:
                    RenderBarSeries(graphics, series, points, brush);
                    break;
                case ChartType.Area:
                    RenderAreaSeries(graphics, points, brush);
                    break;
                case ChartType.Scatter:
                    RenderScatterSeries(graphics, points, brush);
                    break;
                default:
                    RenderLineSeries(graphics, points, pen);
                    break;
            }
        }

        private void RenderLineSeries(Graphics graphics, PointF[] points, Pen pen)
        {
            if (points.Length < 2)
                return;

            graphics.DrawCurve(pen, points);

            // 绘制数据点
            foreach (var point in points)
            {
                using var pointBrush = new SolidBrush(pen.Color);
                graphics.FillEllipse(pointBrush, point.X - 3, point.Y - 3, 6, 6);
            }
        }

        private void RenderBarSeries(Graphics graphics, ChartSeries series, PointF[] points, Brush brush)
        {
            var barWidth = Math.Max(1, _chartArea.Width / (series.Points.Count * 2));

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var barHeight = _chartArea.Bottom - point.Y;
                var barRect = new Rectangle(
                    (int)(point.X - barWidth / 2),
                    (int)point.Y,
                    (int)barWidth,
                    (int)barHeight * (float)_animationProgress
                );

                graphics.FillRectangle(brush, barRect);
            }
        }

        private void RenderAreaSeries(Graphics graphics, PointF[] points, Brush brush)
        {
            if (points.Length < 2)
                return;

            var areaPoints = new List<PointF>(points);
            areaPoints.Add(new PointF(points[points.Length - 1].X, _chartArea.Bottom));
            areaPoints.Add(new PointF(points[0].X, _chartArea.Bottom));

            graphics.FillPolygon(brush, areaPoints.ToArray());

            // 绘制边框
            using var pen = new Pen(brush, 1);
            graphics.DrawLines(pen, points);
        }

        private void RenderScatterSeries(Graphics graphics, PointF[] points, Brush brush)
        {
            foreach (var point in points)
            {
                var size = 6 * (float)_animationProgress;
                var rect = new RectangleF(
                    point.X - size / 2,
                    point.Y - size / 2,
                    size,
                    size
                );
                graphics.FillEllipse(brush, rect);
            }
        }

        private void RenderGrid(Graphics graphics)
        {
            using var gridPen = new Pen(GetThemeColor(ThemeColor.GridColor), 1)
            {
                DashPattern = new float[] { 2, 2 }
            };

            // 水平网格线
            for (int i = 0; i <= 10; i++)
            {
                var y = _chartArea.Y + (_chartArea.Height * i / 10);
                graphics.DrawLine(gridPen, _chartArea.Left, y, _chartArea.Right, y);
            }

            // 垂直网格线
            for (int i = 0; i <= 10; i++)
            {
                var x = _chartArea.X + (_chartArea.Width * i / 10);
                graphics.DrawLine(gridPen, x, _chartArea.Top, x, _chartArea.Bottom);
            }
        }

        private void RenderAxes(Graphics graphics)
        {
            using var axisPen = new Pen(GetThemeColor(ThemeColor.AxisColor), 2);
            using var axisBrush = new SolidBrush(GetThemeColor(ThemeColor.TextColor));
            using var axisFont = new Font("Arial", 9);

            // X轴
            graphics.DrawLine(axisPen, _chartArea.Left, _chartArea.Bottom, _chartArea.Right, _chartArea.Bottom);

            // Y轴
            graphics.DrawLine(axisPen, _chartArea.Left, _chartArea.Top, _chartArea.Left, _chartArea.Bottom);

            // 轴标签
            if (_chartData.XAxisLabel != null)
            {
                var xLabelSize = graphics.MeasureString(_chartData.XAxisLabel, axisFont);
                var xLabelPos = new PointF(
                    _chartArea.X + _chartArea.Width / 2 - xLabelSize.Width / 2,
                    _chartArea.Bottom + 10
                );
                graphics.DrawString(_chartData.XAxisLabel, axisFont, axisBrush, xLabelPos);
            }

            if (_chartData.YAxisLabel != null)
            {
                var yLabelSize = graphics.MeasureString(_chartData.YAxisLabel, axisFont);
                var yLabelPos = new PointF(
                    _chartArea.Left - yLabelSize.Height - 10,
                    _chartArea.Y + _chartArea.Height / 2 - yLabelSize.Width / 2
                );

                graphics.Save();
                graphics.TranslateTransform(yLabelPos.X, yLabelPos.Y);
                graphics.RotateTransform(-90);
                graphics.DrawString(_chartData.YAxisLabel, axisFont, axisBrush, 0, 0);
                graphics.Restore();
            }
        }

        private void RenderTitle(Graphics graphics)
        {
            using var titleFont = new Font("Arial", 14, FontStyle.Bold);
            using var titleBrush = new SolidBrush(GetThemeColor(ThemeColor.TextColor));

            var titleSize = graphics.MeasureString(_chartData.Title, titleFont);
            var titlePos = new PointF(
                _titleArea.X + _titleArea.Width / 2 - titleSize.Width / 2,
                _titleArea.Y + _titleArea.Height / 2 - titleSize.Height / 2
            );

            graphics.DrawString(_chartData.Title, titleFont, titleBrush, titlePos);
        }

        private void RenderLegend(Graphics graphics)
        {
            using var legendFont = new Font("Arial", 9);
            using var legendBrush = new SolidBrush(GetThemeColor(ThemeColor.TextColor));

            var y = _legendArea.Y + 10;

            foreach (var series in _chartData.Series)
            {
                // 图例色块
                using var seriesBrush = new SolidBrush(series.Color);
                var colorRect = new Rectangle(_legendArea.X + 10, y, 15, 15);
                graphics.FillRectangle(seriesBrush, colorRect);
                graphics.DrawRectangle(Pens.Black, colorRect);

                // 图例文本
                var textPos = new PointF(_legendArea.X + 30, y);
                graphics.DrawString(series.Name, legendFont, legendBrush, textPos);

                y += 25;
            }
        }

        private PointF ScreenToChart(Point screenPoint)
        {
            return new PointF(
                (screenPoint.X - _panOffset.X) / _zoomFactor,
                (screenPoint.Y - _panOffset.Y) / _zoomFactor
            );
        }

        private PointF ChartToScreen(PointF chartPoint)
        {
            return new PointF(
                chartPoint.X * _zoomFactor + _panOffset.X,
                chartPoint.Y * _zoomFactor + _panOffset.Y
            );
        }

        private Color GetThemeColor(ThemeColor colorType)
        {
            return _theme switch
            {
                ChartTheme.Dark => colorType switch
                {
                    ThemeColor.BackgroundColor => Color.FromArgb(45, 45, 48),
                    ThemeColor.TextColor => Color.White,
                    ThemeColor.GridColor => Color.FromArgb(80, 80, 80),
                    ThemeColor.AxisColor => Color.FromArgb(150, 150, 150),
                    _ => Color.White
                },
                ChartTheme.Light => colorType switch
                {
                    ThemeColor.BackgroundColor => Color.White,
                    ThemeColor.TextColor => Color.Black,
                    ThemeColor.GridColor => Color.FromArgb(230, 230, 230),
                    ThemeColor.AxisColor => Color.Black,
                    _ => Color.Black
                },
                ChartTheme.Professional => colorType switch
                {
                    ThemeColor.BackgroundColor => Color.FromArgb(248, 248, 248),
                    ThemeColor.TextColor => Color.FromArgb(32, 32, 32),
                    ThemeColor.GridColor => Color.FromArgb(220, 220, 220),
                    ThemeColor.AxisColor => Color.FromArgb(100, 100, 100),
                    _ => Color.Black
                },
                ChartTheme.Colorful => colorType switch
                {
                    ThemeColor.BackgroundColor => Color.FromArgb(255, 250, 240),
                    ThemeColor.TextColor => Color.FromArgb(51, 51, 51),
                    ThemeColor.GridColor => Color.FromArgb(240, 230, 210),
                    ThemeColor.AxisColor => Color.FromArgb(139, 90, 43),
                    _ => Color.Black
                },
                _ => colorType switch
                {
                    ThemeColor.BackgroundColor => Color.White,
                    ThemeColor.TextColor => Color.Black,
                    ThemeColor.GridColor => Color.LightGray,
                    ThemeColor.AxisColor => Color.Black,
                    _ => Color.Black
                }
            };
        }

        private void InvalidateCache()
        {
            ClearCache();
            _needsRedraw = true;
        }

        private void ClearCache()
        {
            foreach (var bitmap in _seriesCache.Values)
            {
                bitmap?.Dispose();
            }
            _seriesCache.Clear();

            foreach (var path in _pathCache.Values)
            {
                path?.Dispose();
            }
            _pathCache.Clear();
        }

        private RectangleF CalculateDataBounds()
        {
            var bounds = RectangleF.Empty;

            foreach (var series in _chartData.Series)
            {
                foreach (var point in series.Points)
                {
                    var pointBounds = new RectangleF(point.X, point.Y, 0, 0);
                    if (bounds.IsEmpty)
                        bounds = pointBounds;
                    else
                        bounds = RectangleF.Union(bounds, pointBounds);
                }
            }

            return bounds;
        }

        #endregion

        #region 事件触发器

        protected virtual void OnRenderStart(RenderStartEventArgs e)
        {
            RenderStart?.Invoke(this, e);
        }

        protected virtual void OnRenderComplete(RenderCompleteEventArgs e)
        {
            RenderComplete?.Invoke(this, e);
        }

        protected virtual void OnRenderModeChanged(RenderModeChangedEventArgs e)
        {
            RenderModeChanged?.Invoke(this, e);
        }

        protected virtual void OnDataPointHover(DataPointHoverEventArgs e)
        {
            DataPointHover?.Invoke(this, e);
        }

        protected virtual void OnDataPointClick(DataPointClickEventArgs e)
        {
            DataPointClick?.Invoke(this, e);
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 图表数据
    /// </summary>
    public class ChartData
    {
        public string Title { get; set; } = "";
        public string XAxisLabel { get; set; } = "";
        public string YAxisLabel { get; set; } = "";
        public List<ChartSeries> Series { get; set; } = new List<ChartSeries>();
    }

    /// <summary>
    /// 图表数据系列
    /// </summary>
    public class ChartSeries
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; } = Color.Blue;
        public ChartType Type { get; set; } = ChartType.Line;
        public List<ChartDataPoint> Points { get; set; } = new List<ChartDataPoint>();
    }

    /// <summary>
    /// 图表数据点
    /// </summary>
    public class ChartDataPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Value { get; set; }
        public string Label { get; set; } = "";
        public ChartSeries Series { get; set; }
    }

    /// <summary>
    /// 渲染路径（用于缓存复杂路径）
    /// </summary>
    public class RenderPath : IDisposable
    {
        public GraphicsPath Path { get; set; }
        public Pen Pen { get; set; }
        public Brush Brush { get; set; }

        public RenderPath(GraphicsPath path, Pen pen, Brush brush)
        {
            Path = path;
            Pen = pen;
            Brush = brush;
        }

        public void Dispose()
        {
            Path?.Dispose();
            Pen?.Dispose();
            Brush?.Dispose();
        }
    }

    /// <summary>
    /// 主题颜色类型
    /// </summary>
    private enum ThemeColor
    {
        BackgroundColor,
        TextColor,
        GridColor,
        AxisColor
    }

    #endregion

    #region 事件参数类

    /// <summary>
    /// 渲染开始事件参数
    /// </summary>
    public class RenderStartEventArgs : EventArgs
    {
    }

    /// <summary>
    /// 渲染完成事件参数
    /// </summary>
    public class RenderCompleteEventArgs : EventArgs
    {
        public TimeSpan RenderTime { get; }

        public RenderCompleteEventArgs(TimeSpan renderTime)
        {
            RenderTime = renderTime;
        }
    }

    /// <summary>
    /// 渲染模式改变事件参数
    /// </summary>
    public class RenderModeChangedEventArgs : EventArgs
    {
        public ChartRenderer.RenderMode OldMode { get; }
        public ChartRenderer.RenderMode NewMode { get; }

        public RenderModeChangedEventArgs(ChartRenderer.RenderMode oldMode, ChartRenderer.RenderMode newMode)
        {
            OldMode = oldMode;
            NewMode = newMode;
        }
    }

    /// <summary>
    /// 数据点悬停事件参数
    /// </summary>
    public class DataPointHoverEventArgs : EventArgs
    {
        public ChartDataPoint DataPoint { get; }

        public DataPointHoverEventArgs(ChartDataPoint dataPoint)
        {
            DataPoint = dataPoint;
        }
    }

    /// <summary>
    /// 数据点点击事件参数
    /// </summary>
    public class DataPointClickEventArgs : EventArgs
    {
        public ChartDataPoint DataPoint { get; }
        public Point Location { get; }

        public DataPointClickEventArgs(ChartDataPoint dataPoint, Point location)
        {
            DataPoint = dataPoint;
            Location = location;
        }
    }

    #endregion
}