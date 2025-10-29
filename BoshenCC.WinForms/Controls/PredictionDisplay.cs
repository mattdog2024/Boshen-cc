using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoshenCC.Core.Utils;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 预测线显示控件
    /// 实现高级的预测线可视化显示，支持多种显示模式、动态效果和专业的图表呈现
    /// </summary>
    public class PredictionDisplay : Control
    {
        #region 事件

        /// <summary>
        /// 预测线悬停事件
        /// </summary>
        public event EventHandler<PredictionLineHoverEventArgs> PredictionLineHover;

        /// <summary>
        /// 预测线点击事件
        /// </summary>
        public event EventHandler<PredictionLineClickEventArgs> PredictionLineClick;

        /// <summary>
        /// 渲染状态改变事件
        /// </summary>
        public event EventHandler<RenderStateChangedEventArgs> RenderStateChanged;

        #endregion

        #region 私有字段

        private List<PredictionLine> _predictionLines;
        private PredictionRenderOptions _renderOptions;
        private PriceRange _priceRange;
        private Rectangle _chartArea;
        private CoordinateHelper _coordinateHelper;
        private PredictionLine _hoveredLine;
        private PredictionLine _selectedLine;
        private bool _isAnimating;
        private double _animationProgress;
        private DateTime _animationStartTime;
        private TimeSpan _animationDuration = TimeSpan.FromMilliseconds(800);
        private readonly Timer _animationTimer;
        private readonly Dictionary<PredictionLine, float> _targetPositions;
        private readonly Dictionary<PredictionLine, float> _currentPositions;
        private Bitmap _backBuffer;
        private bool _needsRedraw;

        // 渲染资源
        private readonly SolidBrush _backgroundBrush;
        private readonly Pen _gridPen;
        private readonly Font _labelFont;
        private readonly SolidBrush _labelBrush;
        private readonly SolidBrush _highlightBrush;

        #endregion

        #region 枚举

        /// <summary>
        /// 显示模式
        /// </summary>
        public enum DisplayMode
        {
            /// <summary>
            /// 标准模式
            /// </summary>
            Standard,

            /// <summary>
            /// 高对比度模式
            /// </summary>
            HighContrast,

            /// <summary>
            /// 简约模式
            /// </summary>
            Minimal,

            /// <summary>
            /// 专业模式
            /// </summary>
            Professional
        }

        /// <summary>
        /// 动画模式
        /// </summary>
        public enum AnimationMode
        {
            /// <summary>
            /// 无动画
            /// </summary>
            None,

            /// <summary>
            /// 淡入动画
            /// </summary>
            FadeIn,

            /// <summary>
            /// 滑入动画
            /// </summary>
            SlideIn,

            /// <summary>
            /// 缩放动画
            /// </summary>
            Scale,

            /// <summary>
            /// 波浪动画
            /// </summary>
            Wave
        }

        #endregion

        #region 构造函数

        public PredictionDisplay()
        {
            InitializeComponent();

            _predictionLines = new List<PredictionLine>();
            _renderOptions = PredictionRenderer.CreateDefaultOptions();
            _priceRange = new PriceRange();
            _coordinateHelper = new CoordinateHelper();
            _targetPositions = new Dictionary<PredictionLine, float>();
            _currentPositions = new Dictionary<PredictionLine, float>();
            _animationTimer = new Timer();
            _animationTimer.Tick += OnAnimationTimerTick;

            // 初始化渲染资源
            _backgroundBrush = new SolidBrush(Color.White);
            _gridPen = new Pen(Color.FromArgb(200, 200, 200), 1) { DashPattern = new float[] { 2, 2 } };
            _labelFont = new Font("Microsoft YaHei", 9, FontStyle.Regular);
            _labelBrush = new SolidBrush(Color.Black);
            _highlightBrush = new SolidBrush(Color.FromArgb(100, Color.LightBlue));

            // 启用双缓冲和高性能渲染
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 预测线列表
        /// </summary>
        public List<PredictionLine> PredictionLines
        {
            get => _predictionLines;
            set
            {
                if (_predictionLines != value)
                {
                    _predictionLines = value ?? new List<PredictionLine>();
                    UpdatePriceRange();
                    StartAnimation();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 渲染选项
        /// </summary>
        public PredictionRenderOptions RenderOptions
        {
            get => _renderOptions;
            set
            {
                if (_renderOptions != value)
                {
                    _renderOptions = value ?? PredictionRenderer.CreateDefaultOptions();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 显示模式
        /// </summary>
        public DisplayMode CurrentDisplayMode { get; set; } = DisplayMode.Standard;

        /// <summary>
        /// 动画模式
        /// </summary>
        public AnimationMode CurrentAnimationMode { get; set; } = AnimationMode.SlideIn;

        /// <summary>
        /// 价格范围
        /// </summary>
        public PriceRange PriceRange
        {
            get => _priceRange;
            set
            {
                if (_priceRange.MinPrice != value.MinPrice || _priceRange.MaxPrice != value.MaxPrice)
                {
                    _priceRange = value ?? new PriceRange();
                    UpdateCoordinateHelper();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 图表区域
        /// </summary>
        public Rectangle ChartArea
        {
            get => _chartArea;
            private set
            {
                if (_chartArea != value)
                {
                    _chartArea = value;
                    UpdateCoordinateHelper();
                }
            }
        }

        /// <summary>
        /// 当前悬停的预测线
        /// </summary>
        public PredictionLine HoveredLine
        {
            get => _hoveredLine;
            private set
            {
                if (_hoveredLine != value)
                {
                    _hoveredLine = value;
                    OnPredictionLineHover(new PredictionLineHoverEventArgs(_hoveredLine));
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 当前选中的预测线
        /// </summary>
        public PredictionLine SelectedLine
        {
            get => _selectedLine;
            set
            {
                if (_selectedLine != value)
                {
                    _selectedLine = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否正在动画中
        /// </summary>
        public bool IsAnimating => _isAnimating;

        /// <summary>
        /// 是否显示网格
        /// </summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>
        /// 是否显示价格轴
        /// </summary>
        public bool ShowPriceAxis { get; set; } = true;

        /// <summary>
        /// 是否启用交互
        /// </summary>
        public bool EnableInteraction { get; set; } = true;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置预测线并启动动画显示
        /// </summary>
        /// <param name="lines">预测线列表</param>
        public async Task SetPredictionLinesAsync(List<PredictionLine> lines)
        {
            await Task.Run(() =>
            {
                PredictionLines = lines;
            });
        }

        /// <summary>
        /// 添加单条预测线
        /// </summary>
        /// <param name="line">预测线</param>
        public void AddPredictionLine(PredictionLine line)
        {
            if (line != null && !_predictionLines.Contains(line))
            {
                _predictionLines.Add(line);
                UpdatePriceRange();
                StartLineAnimation(line);
                Invalidate();
            }
        }

        /// <summary>
        /// 移除预测线
        /// </summary>
        /// <param name="line">预测线</param>
        public void RemovePredictionLine(PredictionLine line)
        {
            if (line != null && _predictionLines.Contains(line))
            {
                _predictionLines.Remove(line);
                _targetPositions.Remove(line);
                _currentPositions.Remove(line);
                UpdatePriceRange();
                Invalidate();
            }
        }

        /// <summary>
        /// 清除所有预测线
        /// </summary>
        public void ClearPredictionLines()
        {
            if (_predictionLines.Count > 0)
            {
                _predictionLines.Clear();
                _targetPositions.Clear();
                _currentPositions.Clear();
                _hoveredLine = null;
                _selectedLine = null;
                Invalidate();
            }
        }

        /// <summary>
        /// 设置显示模式
        /// </summary>
        /// <param name="mode">显示模式</param>
        public void SetDisplayMode(DisplayMode mode)
        {
            CurrentDisplayMode = mode;
            RenderOptions = mode switch
            {
                DisplayMode.HighContrast => PredictionRenderer.CreateHighContrastOptions(),
                DisplayMode.Minimal => PredictionRenderer.CreateMinimalOptions(),
                DisplayMode.Professional => CreateProfessionalOptions(),
                _ => PredictionRenderer.CreateDefaultOptions()
            };
        }

        /// <summary>
        /// 开始动画
        /// </summary>
        public void StartAnimation()
        {
            if (CurrentAnimationMode == AnimationMode.None)
            {
                // 直接设置最终位置
                foreach (var line in _predictionLines)
                {
                    var targetPos = CalculateLinePosition(line);
                    _currentPositions[line] = targetPos;
                    _targetPositions[line] = targetPos;
                }
                return;
            }

            _isAnimating = true;
            _animationProgress = 0;
            _animationStartTime = DateTime.Now;

            // 计算目标位置
            foreach (var line in _predictionLines)
            {
                var targetPos = CalculateLinePosition(line);
                _targetPositions[line] = targetPos;

                // 设置初始位置（根据动画模式）
                if (!_currentPositions.ContainsKey(line))
                {
                    _currentPositions[line] = GetAnimationStartPosition(line, targetPos);
                }
            }

            _animationTimer.Interval = 16; // ~60 FPS
            _animationTimer.Start();

            OnRenderStateChanged(new RenderStateChangedEventArgs(true, false));
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void StopAnimation()
        {
            _isAnimating = false;
            _animationTimer.Stop();

            // 设置最终位置
            foreach (var kvp in _targetPositions)
            {
                _currentPositions[kvp.Key] = kvp.Value;
            }

            OnRenderStateChanged(new RenderStateChangedEventArgs(false, true));
            Invalidate();
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void RefreshDisplay()
        {
            UpdatePriceRange();
            UpdateCoordinateHelper();
            Invalidate();
        }

        /// <summary>
        /// 获取指定位置的预测线
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>预测线，如果没有找到返回null</returns>
        public PredictionLine GetPredictionLineAt(int x, int y)
        {
            if (!EnableInteraction || !ChartArea.Contains(x, y))
                return null;

            const int tolerance = 5; // 5像素的容差

            foreach (var line in _predictionLines)
            {
                if (_currentPositions.TryGetValue(line, out var yPos))
                {
                    if (Math.Abs(yPos - y) <= tolerance)
                    {
                        return line;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 导出为图像
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <returns>图像</returns>
        public Bitmap ExportToImage(int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);

            // 设置高质量渲染
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 绘制背景
            graphics.Clear(Color.White);

            // 计算临时图表区域
            var tempChartArea = new Rectangle(60, 20, width - 100, height - 40);

            // 渲染预测线
            PredictionRenderer.RenderPredictionLines(graphics, _predictionLines, tempChartArea, _priceRange, _renderOptions);

            return bitmap;
        }

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

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
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            ChartArea = CalculateChartArea();
            CreateBackBuffer();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!EnableInteraction)
                return;

            var previousHoveredLine = HoveredLine;
            HoveredLine = GetPredictionLineAt(e.X, e.Y);

            // 更新鼠标光标
            Cursor = HoveredLine != null ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!EnableInteraction || e.Button != MouseButtons.Left)
                return;

            var clickedLine = GetPredictionLineAt(e.X, e.Y);
            if (clickedLine != null)
            {
                SelectedLine = clickedLine;
                OnPredictionLineClick(new PredictionLineClickEventArgs(clickedLine, e.Location));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredLine = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
                _backBuffer?.Dispose();
                _backgroundBrush?.Dispose();
                _gridPen?.Dispose();
                _labelFont?.Dispose();
                _labelBrush?.Dispose();
                _highlightBrush?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region 私有方法

        private void InitializeComponent()
        {
            // 设置控件样式
            BackColor = Color.White;
            ForeColor = Color.Black;
            ResizeRedraw = true;
        }

        private Rectangle CalculateChartArea()
        {
            var padding = 40;
            var rightPadding = ShowPriceAxis ? 80 : 20;

            return new Rectangle(
                padding,
                padding,
                Width - padding - rightPadding,
                Height - padding * 2
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
            graphics.Clear(BackColor);

            // 绘制网格
            if (ShowGrid)
            {
                DrawGrid(graphics);
            }

            // 绘制价格轴
            if (ShowPriceAxis)
            {
                DrawPriceAxis(graphics);
            }

            // 绘制预测线
            DrawPredictionLines(graphics);

            // 绘制高亮效果
            if (HoveredLine != null || SelectedLine != null)
            {
                DrawHighlightEffects(graphics);
            }
        }

        private void DrawGrid(Graphics graphics)
        {
            const int horizontalGridLines = 10;
            const int verticalGridLines = 8;

            // 绘制水平网格线
            for (int i = 0; i <= horizontalGridLines; i++)
            {
                var y = ChartArea.Top + (ChartArea.Height * i / horizontalGridLines);
                graphics.DrawLine(_gridPen, ChartArea.Left, y, ChartArea.Right, y);
            }

            // 绘制垂直网格线
            for (int i = 0; i <= verticalGridLines; i++)
            {
                var x = ChartArea.Left + (ChartArea.Width * i / verticalGridLines);
                graphics.DrawLine(_gridPen, x, ChartArea.Top, x, ChartArea.Bottom);
            }
        }

        private void DrawPriceAxis(Graphics graphics)
        {
            const int labelCount = 8;

            for (int i = 0; i <= labelCount; i++)
            {
                var ratio = (double)i / labelCount;
                var price = _priceRange.MinPrice + (_priceRange.MaxPrice - _priceRange.MinPrice) * (1 - ratio);
                var y = ChartArea.Top + (int)(ChartArea.Height * ratio);

                // 绘制刻度线
                graphics.DrawLine(Pens.Black, ChartArea.Right, y, ChartArea.Right + 5, y);

                // 绘制价格标签
                var priceText = price.ToString("F2");
                var textSize = graphics.MeasureString(priceText, _labelFont);
                var textPosition = new PointF(
                    ChartArea.Right + 10,
                    y - textSize.Height / 2
                );
                graphics.DrawString(priceText, _labelFont, _labelBrush, textPosition);
            }
        }

        private void DrawPredictionLines(Graphics graphics)
        {
            if (_predictionLines.Count == 0)
                return;

            // 按Y位置排序，确保正确的绘制顺序
            var sortedLines = _predictionLines
                .Where(line => _currentPositions.ContainsKey(line))
                .OrderBy(line => _currentPositions[line])
                .ToList();

            foreach (var line in sortedLines)
            {
                DrawSinglePredictionLine(graphics, line);
            }
        }

        private void DrawSinglePredictionLine(Graphics graphics, PredictionLine line)
        {
            if (!_currentPositions.TryGetValue(line, out var yPos))
                return;

            // 计算透明度（动画期间）
            var opacity = _isAnimating ? CalculateAnimationOpacity(line) : line.Opacity;
            var alpha = (int)(opacity * 255);

            // 获取线条颜色
            var lineColor = GetLineColor(line);
            var actualColor = Color.FromArgb(alpha, lineColor);

            // 获取线条宽度
            var lineWidth = GetLineWidth(line);

            // 获取虚线模式
            var dashPattern = GetDashPattern(line);

            // 创建画笔
            using var pen = new Pen(actualColor, lineWidth)
            {
                DashPattern = dashPattern,
                DashStyle = dashPattern != null ? DashStyle.Custom : DashStyle.Solid
            };

            // 绘制线条
            graphics.DrawLine(pen, ChartArea.Left, yPos, ChartArea.Right, yPos);

            // 绘制重点线标记
            if (line.IsKeyLine && _renderOptions.ShowKeyLineMarkers)
            {
                DrawKeyLineMarker(graphics, line, yPos, alpha);
            }

            // 绘制价格标签
            if (_renderOptions.ShowLabels && line.ShowPriceLabel)
            {
                DrawPriceLabel(graphics, line, yPos, alpha);
            }
        }

        private void DrawKeyLineMarker(Graphics graphics, PredictionLine line, float yPos, int alpha)
        {
            const int markerSize = 8;
            var markerX = ChartArea.Left + 20;
            var markerColor = Color.FromArgb(alpha, _renderOptions.Colors.KeyLine);

            using var markerBrush = new SolidBrush(markerColor);
            using var markerPen = new Pen(markerColor, 2);

            // 绘制三角形标记
            var markerPath = new GraphicsPath();
            markerPath.AddPolygon(new Point[]
            {
                new Point(markerX - markerSize / 2, (int)(yPos - markerSize / 2)),
                new Point(markerX + markerSize / 2, (int)(yPos - markerSize / 2)),
                new Point(markerX, (int)(yPos + markerSize / 2))
            });

            graphics.FillPath(markerBrush, markerPath);
            graphics.DrawPath(markerPen, markerPath);
            markerPath.Dispose();
        }

        private void DrawPriceLabel(Graphics graphics, PredictionLine line, float yPos, int alpha)
        {
            var labelText = line.GetPriceLabelText();
            if (string.IsNullOrEmpty(labelText))
                return;

            // 测量文本大小
            var textSize = graphics.MeasureString(labelText, _labelFont);
            var labelBounds = new Rectangle(
                ChartArea.Right - (int)textSize.Width - 15,
                (int)(yPos - textSize.Height / 2) - 2,
                (int)textSize.Width + 10,
                (int)textSize.Height + 4
            );

            // 确保标签在图表范围内
            if (labelBounds.Right > ChartArea.Right)
                labelBounds.X = ChartArea.Right - labelBounds.Width - 5;
            if (labelBounds.Top < ChartArea.Top)
                labelBounds.Top = ChartArea.Top;
            if (labelBounds.Bottom > ChartArea.Bottom)
                labelBounds.Top = ChartArea.Bottom - labelBounds.Height;

            // 绘制标签背景
            var backgroundColor = Color.FromArgb(alpha, _renderOptions.Style.LabelBackgroundColor);
            var borderColor = Color.FromArgb(alpha, _renderOptions.Style.LabelBorderColor);

            using var backgroundBrush = new SolidBrush(backgroundColor);
            using var borderPen = new Pen(borderColor, 1);

            graphics.FillRectangle(backgroundBrush, labelBounds);
            graphics.DrawRectangle(borderPen, labelBounds);

            // 绘制文本
            var textColor = Color.FromArgb(alpha, _labelBrush.Color);
            using var textBrush = new SolidBrush(textColor);

            var textPosition = new Point(
                labelBounds.Left + 5,
                labelBounds.Top + 2
            );
            graphics.DrawString(labelText, _labelFont, textBrush, textPosition);
        }

        private void DrawHighlightEffects(Graphics graphics)
        {
            // 高亮悬停的预测线
            if (HoveredLine != null && _currentPositions.TryGetValue(HoveredLine, out var hoverY))
            {
                var highlightRect = new Rectangle(ChartArea.Left, (int)(hoverY - 10), ChartArea.Width, 20);
                using var highlightBrush = new SolidBrush(Color.FromArgb(50, Color.LightBlue));
                graphics.FillRectangle(highlightBrush, highlightRect);
            }

            // 高亮选中的预测线
            if (SelectedLine != null && _currentPositions.TryGetValue(SelectedLine, out var selectedY))
            {
                var highlightRect = new Rectangle(ChartArea.Left, (int)(selectedY - 15), ChartArea.Width, 30);
                using var highlightPen = new Pen(Color.Blue, 2);
                graphics.DrawRectangle(highlightPen, highlightRect);
            }
        }

        private float CalculateLinePosition(PredictionLine line)
        {
            if (_priceRange.MaxPrice <= _priceRange.MinPrice)
                return ChartArea.Top;

            var ratio = (line.Price - _priceRange.MinPrice) / (_priceRange.MaxPrice - _priceRange.MinPrice);
            var yPosition = ChartArea.Bottom - (float)(ratio * ChartArea.Height);

            return Math.Max(ChartArea.Top, Math.Min(ChartArea.Bottom, yPosition));
        }

        private Color GetLineColor(PredictionLine line)
        {
            if (line.Color != Color.Empty && line.Color != Color.Transparent)
                return line.Color;

            return line.LineType switch
            {
                PredictionLineType.PointA => _renderOptions.Colors.PointALine,
                PredictionLineType.PointB => _renderOptions.Colors.PointBLine,
                PredictionLineType.ExtremeLine => _renderOptions.Colors.ExtremeLine,
                _ => line.IsKeyLine ? _renderOptions.Colors.KeyLine : _renderOptions.Colors.NormalLine
            };
        }

        private int GetLineWidth(PredictionLine line)
        {
            if (line.Width > 0)
                return line.Width;

            return line.LineType switch
            {
                PredictionLineType.ExtremeLine => _renderOptions.Style.ExtremeLineWidth,
                _ => line.IsKeyLine ? _renderOptions.Style.KeyLineWidth : _renderOptions.Style.NormalLineWidth
            };
        }

        private float[] GetDashPattern(PredictionLine line)
        {
            if (line.Index == 0 || line.Index == 1) // A线或B线
                return null;

            return line.Style switch
            {
                PredictionLineStyle.Dashed => new float[] { 8, 4 },
                PredictionLineStyle.Dotted => new float[] { 2, 2 },
                PredictionLineStyle.DashDot => new float[] { 8, 4, 2, 4 },
                _ => _renderOptions.Style.DashPattern
            };
        }

        private void UpdatePriceRange()
        {
            if (_predictionLines.Count == 0)
            {
                _priceRange = new PriceRange { MinPrice = 0, MaxPrice = 100 };
                return;
            }

            var prices = _predictionLines.Select(l => l.Price).ToList();
            var minPrice = prices.Min();
            var maxPrice = prices.Max();

            // 添加边距
            var padding = (maxPrice - minPrice) * 0.1;
            _priceRange = new PriceRange
            {
                MinPrice = minPrice - padding,
                MaxPrice = maxPrice + padding
            };
        }

        private void UpdateCoordinateHelper()
        {
            _coordinateHelper.ImageArea = ChartArea;
            _coordinateHelper.SetPriceRange(_priceRange.MinPrice, _priceRange.MaxPrice, ChartArea);
        }

        private void StartLineAnimation(PredictionLine line)
        {
            if (CurrentAnimationMode == AnimationMode.None)
            {
                var targetPos = CalculateLinePosition(line);
                _currentPositions[line] = targetPos;
                _targetPositions[line] = targetPos;
                return;
            }

            var targetPosition = CalculateLinePosition(line);
            _targetPositions[line] = targetPosition;
            _currentPositions[line] = GetAnimationStartPosition(line, targetPosition);

            if (!_isAnimating)
            {
                StartAnimation();
            }
        }

        private float GetAnimationStartPosition(PredictionLine line, float targetPos)
        {
            return CurrentAnimationMode switch
            {
                AnimationMode.FadeIn => targetPos, // 位置不变，透明度变化
                AnimationMode.SlideIn => ChartArea.Bottom + 50, // 从底部滑入
                AnimationMode.Scale => ChartArea.Center().Y, // 从中心缩放
                AnimationMode.Wave => targetPos + (float)(Math.Sin(line.Index) * 30), // 波浪效果
                _ => targetPos
            };
        }

        private double CalculateAnimationOpacity(PredictionLine line)
        {
            if (CurrentAnimationMode == AnimationMode.FadeIn)
            {
                return line.Opacity * _animationProgress;
            }
            return line.Opacity;
        }

        private float CalculateAnimatedPosition(PredictionLine line)
        {
            if (!_targetPositions.TryGetValue(line, out var targetPos) ||
                !_currentPositions.TryGetValue(line, out var currentPos))
            {
                return CalculateLinePosition(line);
            }

            var easeProgress = EaseInOutCubic(_animationProgress);
            return currentPos + (targetPos - currentPos) * (float)easeProgress;
        }

        private double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }

        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _animationStartTime;
            _animationProgress = Math.Min(1.0, elapsed.TotalMilliseconds / _animationDuration.TotalMilliseconds);

            // 更新所有线条的当前位置
            foreach (var line in _predictionLines)
            {
                if (_targetPositions.ContainsKey(line))
                {
                    var newPos = CalculateAnimatedPosition(line);

                    // 波浪动画的特殊处理
                    if (CurrentAnimationMode == AnimationMode.Wave)
                    {
                        var targetPos = _targetPositions[line];
                        var waveOffset = (float)(Math.Sin(_animationProgress * Math.PI * 2 + line.Index * 0.5) *
                                                (1 - _animationProgress) * 20);
                        newPos = targetPos + waveOffset;
                    }

                    _currentPositions[line] = newPos;
                }
            }

            Invalidate();

            if (_animationProgress >= 1.0)
            {
                StopAnimation();
            }
        }

        private PredictionRenderOptions CreateProfessionalOptions()
        {
            return new PredictionRenderOptions
            {
                Colors = new PredictionLineColors
                {
                    PointALine = Color.FromArgb(100, 100, 100),
                    PointBLine = Color.FromArgb(100, 100, 100),
                    NormalLine = Color.FromArgb(70, 130, 180),
                    KeyLine = Color.FromArgb(255, 140, 0),
                    ExtremeLine = Color.FromArgb(138, 43, 226)
                },
                Style = new PredictionRenderStyle
                {
                    NormalLineWidth = 1,
                    KeyLineWidth = 2,
                    ExtremeLineWidth = 2,
                    LabelFontSize = 10,
                    LabelFontFamily = "Consolas",
                    LabelBackgroundColor = Color.FromArgb(248, 248, 248),
                    LabelBorderColor = Color.FromArgb(180, 180, 180),
                    DashPattern = new float[] { 6, 3 }
                },
                ShowLabels = true,
                ShowKeyLineMarkers = true,
                ShowGroupIndicators = true,
                ShowBackgroundLayer = true
            };
        }

        #endregion

        #region 事件触发器

        protected virtual void OnPredictionLineHover(PredictionLineHoverEventArgs e)
        {
            PredictionLineHover?.Invoke(this, e);
        }

        protected virtual void OnPredictionLineClick(PredictionLineClickEventArgs e)
        {
            PredictionLineClick?.Invoke(this, e);
        }

        protected virtual void OnRenderStateChanged(RenderStateChangedEventArgs e)
        {
            RenderStateChanged?.Invoke(this, e);
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 预测线悬停事件参数
    /// </summary>
    public class PredictionLineHoverEventArgs : EventArgs
    {
        public PredictionLine Line { get; }

        public PredictionLineHoverEventArgs(PredictionLine line)
        {
            Line = line;
        }
    }

    /// <summary>
    /// 预测线点击事件参数
    /// </summary>
    public class PredictionLineClickEventArgs : EventArgs
    {
        public PredictionLine Line { get; }
        public Point Location { get; }

        public PredictionLineClickEventArgs(PredictionLine line, Point location)
        {
            Line = line;
            Location = location;
        }
    }

    /// <summary>
    /// 渲染状态改变事件参数
    /// </summary>
    public class RenderStateChangedEventArgs : EventArgs
    {
        public bool IsAnimating { get; }
        public bool IsCompleted { get; }

        public RenderStateChangedEventArgs(bool isAnimating, bool isCompleted)
        {
            IsAnimating = isAnimating;
            IsCompleted = isCompleted;
        }
    }

    #endregion
}