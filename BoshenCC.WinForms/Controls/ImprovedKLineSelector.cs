using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 改进的K线选择控件
    /// 增加了影线测量、多种测量模式和增强的交互功能
    /// </summary>
    public class ImprovedKLineSelector : Control
    {
        #region 事件

        /// <summary>
        /// A点选择事件
        /// </summary>
        public event EventHandler<PointSelectedEventArgs> PointASelected;

        /// <summary>
        /// B点选择事件
        /// </summary>
        public event EventHandler<PointSelectedEventArgs> PointBSelected;

        /// <summary>
        /// 选择状态改变事件
        /// </summary>
        public event EventHandler<SelectionStateChangedEventArgs> SelectionStateChanged;

        /// <summary>
        /// 影线测量完成事件
        /// </summary>
        public event EventHandler<ShadowMeasurementCompletedEventArgs> ShadowMeasurementCompleted;

        /// <summary>
        /// 测量模式改变事件
        /// </summary>
        public event EventHandler<MeasurementModeChangedEventArgs> MeasurementModeChanged;

        #endregion

        #region 私有字段

        private CoordinateHelper _coordinateHelper;
        private Point? _pointA;
        private Point? _pointB;
        private Point? _shadowTopPoint;
        private Point? _shadowBottomPoint;
        private SelectionState _selectionState;
        private MeasurementMode _measurementMode;
        private bool _isHovering;
        private Point _mousePosition;
        private Image _backgroundImage;
        private List<SelectionHistory> _history;
        private int _historyIndex;
        private bool _showGrid;
        private bool _showCrosshair;
        private bool _showMeasurementInfo;
        private Color _crosshairColor;
        private int _crosshairThickness;
        private Font _measurementFont;
        private Font _tooltipFont;
        private readonly Pen _pointAPen;
        private readonly Pen _pointBPen;
        private readonly Pen _shadowPen;
        private readonly Pen _crosshairPen;
        private readonly Pen _gridPen;
        private readonly SolidBrush _pointABrush;
        private readonly SolidBrush _pointBBrush;
        private readonly SolidBrush _shadowBrush;
        private readonly SolidBrush _tooltipBrush;
        private readonly SolidBrush _measurementBrush;

        #endregion

        #region 枚举

        /// <summary>
        /// 选择状态
        /// </summary>
        public enum SelectionState
        {
            None,           // 未选择
            PointASelected, // 已选择A点
            Complete        // 选择完成
        }

        /// <summary>
        /// 测量模式
        /// </summary>
        public enum MeasurementMode
        {
            Standard,      // 标准模式：A点到B点
            UpperShadow,   // 上影线测量
            LowerShadow,   // 下影线测量
            FullShadow     // 完整影线测量
        }

        #endregion

        #region 构造函数

        public ImprovedKLineSelector()
        {
            InitializeComponent();

            _coordinateHelper = new CoordinateHelper();
            _selectionState = SelectionState.None;
            _measurementMode = MeasurementMode.Standard;
            _history = new List<SelectionHistory>();
            _historyIndex = -1;
            _showGrid = false;
            _showCrosshair = true;
            _showMeasurementInfo = true;
            _crosshairColor = Color.Gray;
            _crosshairThickness = 1;

            // 初始化绘图资源
            _pointAPen = new Pen(Color.Lime, 2);
            _pointBPen = new Pen(Color.Red, 2);
            _shadowPen = new Pen(Color.Orange, 2);
            _crosshairPen = new Pen(_crosshairColor, _crosshairThickness);
            _gridPen = new Pen(Color.FromArgb(200, 200, 200), 1);
            _pointABrush = new SolidBrush(Color.Lime);
            _pointBBrush = new SolidBrush(Color.Red);
            _shadowBrush = new SolidBrush(Color.Orange);
            _tooltipFont = new Font("Arial", 9);
            _measurementFont = new Font("Arial", 8);
            _tooltipBrush = new SolidBrush(Color.Black);
            _measurementBrush = new SolidBrush(Color.DarkBlue);

            // 启用双缓冲
            DoubleBuffered = true;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// A点位置
        /// </summary>
        public Point? PointA
        {
            get => _pointA;
            set
            {
                if (_pointA != value)
                {
                    _pointA = value;
                    UpdateSelectionState();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// B点位置
        /// </summary>
        public Point? PointB
        {
            get => _pointB;
            set
            {
                if (_pointB != value)
                {
                    _pointB = value;
                    UpdateSelectionState();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 影线顶部点位置
        /// </summary>
        public Point? ShadowTopPoint
        {
            get => _shadowTopPoint;
            set
            {
                if (_shadowTopPoint != value)
                {
                    _shadowTopPoint = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 影线底部点位置
        /// </summary>
        public Point? ShadowBottomPoint
        {
            get => _shadowBottomPoint;
            set
            {
                if (_shadowBottomPoint != value)
                {
                    _shadowBottomPoint = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 选择状态
        /// </summary>
        public SelectionState CurrentState
        {
            get => _selectionState;
            private set
            {
                if (_selectionState != value)
                {
                    var oldState = _selectionState;
                    _selectionState = value;
                    OnSelectionStateChanged(new SelectionStateChangedEventArgs(oldState, value));
                }
            }
        }

        /// <summary>
        /// 测量模式
        /// </summary>
        public MeasurementMode CurrentMeasurementMode
        {
            get => _measurementMode;
            set
            {
                if (_measurementMode != value)
                {
                    var oldMode = _measurementMode;
                    _measurementMode = value;
                    OnMeasurementModeChanged(new MeasurementModeChangedEventArgs(oldMode, value));
                    ClearSelection();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 背景图像
        /// </summary>
        public Image BackgroundImage
        {
            get => _backgroundImage;
            set
            {
                if (_backgroundImage != value)
                {
                    _backgroundImage = value;
                    UpdateImageArea();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 坐标转换器
        /// </summary>
        public CoordinateHelper CoordinateHelper
        {
            get => _coordinateHelper;
            set
            {
                _coordinateHelper = value ?? new CoordinateHelper();
                Invalidate();
            }
        }

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
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 是否显示测量信息
        /// </summary>
        public bool ShowMeasurementInfo
        {
            get => _showMeasurementInfo;
            set
            {
                if (_showMeasurementInfo != value)
                {
                    _showMeasurementInfo = value;
                    Invalidate();
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
                    _crosshairPen.Color = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 十字准星粗细
        /// </summary>
        public int CrosshairThickness
        {
            get => _crosshairThickness;
            set
            {
                if (_crosshairThickness != value && value > 0)
                {
                    _crosshairThickness = value;
                    _crosshairPen.Width = value;
                    Invalidate();
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置价格范围
        /// </summary>
        /// <param name="minPrice">最低价格</param>
        /// <param name="maxPrice">最高价格</param>
        public void SetPriceRange(double minPrice, double maxPrice)
        {
            var imageArea = GetImageArea();
            _coordinateHelper.SetPriceRange(minPrice, maxPrice, imageArea);
        }

        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearSelection()
        {
            _pointA = null;
            _pointB = null;
            _shadowTopPoint = null;
            _shadowBottomPoint = null;
            UpdateSelectionState();
            Invalidate();
        }

        /// <summary>
        /// 获取A点价格
        /// </summary>
        /// <returns>A点价格，如果未选择返回null</returns>
        public double? GetPointAPrice()
        {
            if (_pointA.HasValue)
                return _coordinateHelper.YToPrice(_pointA.Value.Y);
            return null;
        }

        /// <summary>
        /// 获取B点价格
        /// </summary>
        /// <returns>B点价格，如果未选择返回null</returns>
        public double? GetPointBPrice()
        {
            if (_pointB.HasValue)
                return _coordinateHelper.YToPrice(_pointB.Value.Y);
            return null;
        }

        /// <summary>
        /// 获取影线顶部价格
        /// </summary>
        /// <returns>影线顶部价格，如果未选择返回null</returns>
        public double? GetShadowTopPrice()
        {
            if (_shadowTopPoint.HasValue)
                return _coordinateHelper.YToPrice(_shadowTopPoint.Value.Y);
            return null;
        }

        /// <summary>
        /// 获取影线底部价格
        /// </summary>
        /// <returns>影线底部价格，如果未选择返回null</returns>
        public double? GetShadowBottomPrice()
        {
            if (_shadowBottomPoint.HasValue)
                return _coordinateHelper.YToPrice(_shadowBottomPoint.Value.Y);
            return null;
        }

        /// <summary>
        /// 检查选择是否完成
        /// </summary>
        /// <returns>是否已完成选择</returns>
        public bool IsSelectionComplete()
        {
            return _measurementMode switch
            {
                MeasurementMode.Standard => _pointA.HasValue && _pointB.HasValue,
                MeasurementMode.UpperShadow => _pointA.HasValue && _shadowTopPoint.HasValue,
                MeasurementMode.LowerShadow => _pointA.HasValue && _shadowBottomPoint.HasValue,
                MeasurementMode.FullShadow => _shadowTopPoint.HasValue && _shadowBottomPoint.HasValue,
                _ => false
            };
        }

        /// <summary>
        /// 执行影线测量
        /// </summary>
        public void PerformShadowMeasurement()
        {
            if (!_pointA.HasValue) return;

            var imageArea = GetImageArea();
            var pointAY = _pointA.Value.Y;

            // 查找K线的顶部和底部
            var shadowTop = FindShadowTop(_pointA.Value.X, pointAY, imageArea);
            var shadowBottom = FindShadowBottom(_pointA.Value.X, pointAY, imageArea);

            if (shadowTop.HasValue && shadowBottom.HasValue)
            {
                _shadowTopPoint = shadowTop.Value;
                _shadowBottomPoint = shadowBottom.Value;

                var topPrice = _coordinateHelper.YToPrice(shadowTop.Value.Y);
                var bottomPrice = _coordinateHelper.YToPrice(shadowBottom.Value.Y);
                var pointAPrice = _coordinateHelper.YToPrice(pointAY);

                var measurementType = GetMeasurementType();
                OnShadowMeasurementCompleted(new ShadowMeasurementCompletedEventArgs(
                    measurementType, shadowTop.Value, shadowBottom.Value, topPrice, bottomPrice, pointAPrice));

                Invalidate();
            }
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public void Undo()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                RestoreFromHistory(_history[_historyIndex]);
            }
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        public void Redo()
        {
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                RestoreFromHistory(_history[_historyIndex]);
            }
        }

        /// <summary>
        /// 检查是否可以撤销
        /// </summary>
        public bool CanUndo() => _historyIndex > 0;

        /// <summary>
        /// 检查是否可以重做
        /// </summary>
        public bool CanRedo() => _historyIndex < _history.Count - 1;

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制背景图像
            if (_backgroundImage != null)
            {
                e.Graphics.DrawImage(_backgroundImage, GetImageArea());
            }

            // 绘制网格
            if (_showGrid)
            {
                DrawGrid(e.Graphics);
            }

            // 绘制测量线
            DrawMeasurementLines(e.Graphics);

            // 绘制选择点
            DrawSelectionPoints(e.Graphics);

            // 绘制测量信息
            if (_showMeasurementInfo)
            {
                DrawMeasurementInfo(e.Graphics);
            }

            // 绘制十字准星
            if (_showCrosshair && _isHovering)
            {
                DrawCrosshair(e.Graphics);
            }

            // 绘制工具提示
            if (_isHovering)
            {
                DrawTooltip(e.Graphics);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                HandleClick(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var wasHovering = _isHovering;
            _mousePosition = e.Location;
            _isHovering = GetImageArea().Contains(e.Location);

            if (wasHovering != _isHovering)
            {
                Invalidate();
            }

            // 更新鼠标光标
            Cursor = _isHovering ? Cursors.Cross : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isHovering)
            {
                _isHovering = false;
                Invalidate();
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            // 快捷键支持
            switch (char.ToLower(e.KeyChar))
            {
                case '1':
                    CurrentMeasurementMode = MeasurementMode.Standard;
                    break;
                case '2':
                    CurrentMeasurementMode = MeasurementMode.UpperShadow;
                    break;
                case '3':
                    CurrentMeasurementMode = MeasurementMode.LowerShadow;
                    break;
                case '4':
                    CurrentMeasurementMode = MeasurementMode.FullShadow;
                    break;
                case 'm':
                    PerformShadowMeasurement();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pointAPen?.Dispose();
                _pointBPen?.Dispose();
                _shadowPen?.Dispose();
                _crosshairPen?.Dispose();
                _gridPen?.Dispose();
                _pointABrush?.Dispose();
                _pointBBrush?.Dispose();
                _shadowBrush?.Dispose();
                _tooltipFont?.Dispose();
                _measurementFont?.Dispose();
                _tooltipBrush?.Dispose();
                _measurementBrush?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region 私有方法

        private void InitializeComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
        }

        private Rectangle GetImageArea()
        {
            if (_backgroundImage == null)
                return ClientRectangle;

            // 居中显示图像，保持宽高比
            var imageRect = new Rectangle(0, 0, _backgroundImage.Width, _backgroundImage.Height);
            var clientRect = ClientRectangle;

            float scaleX = (float)clientRect.Width / imageRect.Width;
            float scaleY = (float)clientRect.Height / imageRect.Height;
            float scale = Math.Min(scaleX, scaleY);

            int scaledWidth = (int)(imageRect.Width * scale);
            int scaledHeight = (int)(imageRect.Height * scale);

            int x = (clientRect.Width - scaledWidth) / 2;
            int y = (clientRect.Height - scaledHeight) / 2;

            return new Rectangle(x, y, scaledWidth, scaledHeight);
        }

        private void UpdateImageArea()
        {
            var imageArea = GetImageArea();
            _coordinateHelper.ImageArea = imageArea;
        }

        private void HandleClick(Point location)
        {
            if (!_coordinateHelper.IsValidCoordinate(location.X, location.Y))
                return;

            SaveToHistory();

            switch (_measurementMode)
            {
                case MeasurementMode.Standard:
                    HandleStandardModeClick(location);
                    break;
                case MeasurementMode.UpperShadow:
                    HandleUpperShadowModeClick(location);
                    break;
                case MeasurementMode.LowerShadow:
                    HandleLowerShadowModeClick(location);
                    break;
                case MeasurementMode.FullShadow:
                    HandleFullShadowModeClick(location);
                    break;
            }

            UpdateSelectionState();
            Invalidate();
        }

        private void HandleStandardModeClick(Point location)
        {
            switch (_selectionState)
            {
                case SelectionState.None:
                    _pointA = location;
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;

                case SelectionState.PointASelected:
                    _pointB = location;
                    OnPointBSelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;

                case SelectionState.Complete:
                    ClearSelection();
                    _pointA = location;
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;
            }
        }

        private void HandleUpperShadowModeClick(Point location)
        {
            switch (_selectionState)
            {
                case SelectionState.None:
                    _pointA = location;
                    PerformShadowMeasurement();
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;

                case SelectionState.Complete:
                    ClearSelection();
                    _pointA = location;
                    PerformShadowMeasurement();
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;
            }
        }

        private void HandleLowerShadowModeClick(Point location)
        {
            switch (_selectionState)
            {
                case SelectionState.None:
                    _pointA = location;
                    PerformShadowMeasurement();
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;

                case SelectionState.Complete:
                    ClearSelection();
                    _pointA = location;
                    PerformShadowMeasurement();
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;
            }
        }

        private void HandleFullShadowModeClick(Point location)
        {
            switch (_selectionState)
            {
                case SelectionState.None:
                    _shadowTopPoint = location;
                    break;

                case SelectionState.PointASelected:
                    _shadowBottomPoint = location;
                    PerformShadowMeasurement();
                    break;

                case SelectionState.Complete:
                    ClearSelection();
                    _shadowTopPoint = location;
                    break;
            }
        }

        private void UpdateSelectionState()
        {
            if (IsSelectionComplete())
            {
                CurrentState = SelectionState.Complete;
            }
            else if (_measurementMode == MeasurementMode.FullShadow ? _shadowTopPoint.HasValue : _pointA.HasValue)
            {
                CurrentState = SelectionState.PointASelected;
            }
            else
            {
                CurrentState = SelectionState.None;
            }
        }

        private void DrawGrid(Graphics g)
        {
            var imageArea = GetImageArea();
            _gridPen.DashStyle = DashStyle.Dot;

            // 绘制垂直网格线
            for (int x = imageArea.Left; x <= imageArea.Right; x += 50)
            {
                g.DrawLine(_gridPen, x, imageArea.Top, x, imageArea.Bottom);
            }

            // 绘制水平网格线
            for (int y = imageArea.Top; y <= imageArea.Bottom; y += 50)
            {
                g.DrawLine(_gridPen, imageArea.Left, y, imageArea.Right, y);
            }
        }

        private void DrawMeasurementLines(Graphics g)
        {
            switch (_measurementMode)
            {
                case MeasurementMode.Standard:
                    DrawStandardMeasurementLine(g);
                    break;
                case MeasurementMode.UpperShadow:
                    DrawUpperShadowMeasurementLine(g);
                    break;
                case MeasurementMode.LowerShadow:
                    DrawLowerShadowMeasurementLine(g);
                    break;
                case MeasurementMode.FullShadow:
                    DrawFullShadowMeasurementLine(g);
                    break;
            }
        }

        private void DrawStandardMeasurementLine(g)
        {
            if (_pointA.HasValue && _pointB.HasValue)
            {
                _shadowPen.Color = Color.Blue;
                g.DrawLine(_shadowPen, _pointA.Value, _pointB.Value);
            }
        }

        private void DrawUpperShadowMeasurementLine(g)
        {
            if (_pointA.HasValue && _shadowTopPoint.HasValue)
            {
                _shadowPen.Color = Color.Orange;
                g.DrawLine(_shadowPen, _pointA.Value, _shadowTopPoint.Value);
            }
        }

        private void DrawLowerShadowMeasurementLine(g)
        {
            if (_pointA.HasValue && _shadowBottomPoint.HasValue)
            {
                _shadowPen.Color = Color.Purple;
                g.DrawLine(_shadowPen, _pointA.Value, _shadowBottomPoint.Value);
            }
        }

        private void DrawFullShadowMeasurementLine(g)
        {
            if (_shadowTopPoint.HasValue && _shadowBottomPoint.HasValue)
            {
                _shadowPen.Color = Color.DarkGreen;
                g.DrawLine(_shadowPen, _shadowTopPoint.Value, _shadowBottomPoint.Value);
            }
        }

        private void DrawSelectionPoints(g)
        {
            const int pointSize = 8;

            // 绘制标准模式的点
            if (_pointA.HasValue)
            {
                DrawPoint(g, _pointA.Value, _pointAPen, _pointABrush, "A");
            }

            if (_pointB.HasValue)
            {
                DrawPoint(g, _pointB.Value, _pointBPen, _pointBBrush, "B");
            }

            // 绘制影线模式的点
            if (_shadowTopPoint.HasValue)
            {
                DrawPoint(g, _shadowTopPoint.Value, _shadowPen, _shadowBrush, "顶");
            }

            if (_shadowBottomPoint.HasValue)
            {
                DrawPoint(g, _shadowBottomPoint.Value, _shadowPen, _shadowBrush, "底");
            }
        }

        private void DrawPoint(Graphics g, Point point, Pen pen, SolidBrush brush, string label)
        {
            const int pointSize = 8;
            var rect = new Rectangle(
                point.X - pointSize / 2,
                point.Y - pointSize / 2,
                pointSize,
                pointSize);

            g.FillEllipse(brush, rect);
            g.DrawEllipse(pen, rect);

            // 绘制标签
            var labelSize = g.MeasureString(label, _tooltipFont);
            g.DrawString(label, _tooltipFont, brush,
                point.X + pointSize, point.Y - labelSize.Height);
        }

        private void DrawMeasurementInfo(g)
        {
            var info = GetMeasurementInfo();
            if (string.IsNullOrEmpty(info)) return;

            var textSize = g.MeasureString(info, _measurementFont);
            var infoRect = new Rectangle(10, 10, (int)textSize.Width + 10, (int)textSize.Height + 6);

            using (var brush = new SolidBrush(Color.FromArgb(240, Color.White)))
            using (var pen = new Pen(Color.Gray))
            {
                g.FillRectangle(brush, infoRect);
                g.DrawRectangle(pen, infoRect);
            }

            g.DrawString(info, _measurementFont, _measurementBrush, infoRect.X + 5, infoRect.Y + 3);
        }

        private string GetMeasurementInfo()
        {
            switch (_measurementMode)
            {
                case MeasurementMode.Standard:
                    if (_pointA.HasValue && _pointB.HasValue)
                    {
                        var priceA = _coordinateHelper.YToPrice(_pointA.Value.Y);
                        var priceB = _coordinateHelper.YToPrice(_pointB.Value.Y);
                        var distance = Math.Abs(priceB - priceA);
                        return $"A:{priceA:F2} B:{priceB:F2} 距离:{distance:F2}";
                    }
                    break;

                case MeasurementMode.UpperShadow:
                    if (_shadowTopPoint.HasValue)
                    {
                        var topPrice = _coordinateHelper.YToPrice(_shadowTopPoint.Value.Y);
                        return $"上影线顶部:{topPrice:F2}";
                    }
                    break;

                case MeasurementMode.LowerShadow:
                    if (_shadowBottomPoint.HasValue)
                    {
                        var bottomPrice = _coordinateHelper.YToPrice(_shadowBottomPoint.Value.Y);
                        return $"下影线底部:{bottomPrice:F2}";
                    }
                    break;

                case MeasurementMode.FullShadow:
                    if (_shadowTopPoint.HasValue && _shadowBottomPoint.HasValue)
                    {
                        var topPrice = _coordinateHelper.YToPrice(_shadowTopPoint.Value.Y);
                        var bottomPrice = _coordinateHelper.YToPrice(_shadowBottomPoint.Value.Y);
                        var shadowLength = Math.Abs(topPrice - bottomPrice);
                        return $"影线:{topPrice:F2}-{bottomPrice:F2} 长度:{shadowLength:F2}";
                    }
                    break;
            }

            return string.Empty;
        }

        private void DrawCrosshair(g)
        {
            var imageArea = GetImageArea();
            if (!imageArea.Contains(_mousePosition)) return;

            _crosshairPen.Color = _crosshairColor;
            _crosshairPen.Width = _crosshairThickness;

            // 绘制垂直线
            g.DrawLine(_crosshairPen, _mousePosition.X, imageArea.Top, _mousePosition.X, imageArea.Bottom);
            // 绘制水平线
            g.DrawLine(_crosshairPen, imageArea.Left, _mousePosition.Y, imageArea.Right, _mousePosition.Y);
        }

        private void DrawTooltip(g)
        {
            if (_coordinateHelper.IsValidCoordinate(_mousePosition.X, _mousePosition.Y))
            {
                var priceText = _coordinateHelper.GetPriceTooltip(_mousePosition.Y);
                var textSize = g.MeasureString(priceText, _tooltipFont);

                var tooltipRect = new Rectangle(
                    _mousePosition.X + 10,
                    _mousePosition.Y - (int)textSize.Height - 10,
                    (int)textSize.Width + 8,
                    (int)textSize.Height + 4);

                // 确保工具提示不超出控件边界
                if (tooltipRect.Right > Width)
                    tooltipRect.X = _mousePosition.X - (int)textSize.Width - 18;
                if (tooltipRect.Top < 0)
                    tooltipRect.Y = _mousePosition.Y + 10;

                // 绘制背景
                using (var brush = new SolidBrush(Color.FromArgb(240, Color.White)))
                using (var pen = new Pen(Color.Gray))
                {
                    g.FillRectangle(brush, tooltipRect);
                    g.DrawRectangle(pen, tooltipRect);
                }

                // 绘制文本
                g.DrawString(priceText, _tooltipFont, _tooltipBrush, tooltipRect.X + 4, tooltipRect.Y + 2);
            }
        }

        private Point? FindShadowTop(int centerX, int centerY, Rectangle imageArea)
        {
            // 向上搜索，找到K线的顶部
            for (int y = centerY; y >= imageArea.Top; y -= 2)
            {
                // 这里应该基于图像分析找到真正的K线顶部
                // 简化实现，假设向上50像素是影线顶部
                if (y <= centerY - 50)
                    return new Point(centerX, y);
            }
            return null;
        }

        private Point? FindShadowBottom(int centerX, int centerY, Rectangle imageArea)
        {
            // 向下搜索，找到K线的底部
            for (int y = centerY; y <= imageArea.Bottom; y += 2)
            {
                // 这里应该基于图像分析找到真正的K线底部
                // 简化实现，假设向下50像素是影线底部
                if (y >= centerY + 50)
                    return new Point(centerX, y);
            }
            return null;
        }

        private string GetMeasurementType()
        {
            return _measurementMode switch
            {
                MeasurementMode.Standard => "标准测量",
                MeasurementMode.UpperShadow => "上影线测量",
                MeasurementMode.LowerShadow => "下影线测量",
                MeasurementMode.FullShadow => "完整影线测量",
                _ => "未知测量"
            };
        }

        private void SaveToHistory()
        {
            var history = new SelectionHistory
            {
                PointA = _pointA,
                PointB = _pointB,
                ShadowTopPoint = _shadowTopPoint,
                ShadowBottomPoint = _shadowBottomPoint,
                MeasurementMode = _measurementMode,
                SelectionState = _selectionState
            };

            // 移除当前索引之后的历史记录
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            _history.Add(history);
            _historyIndex = _history.Count - 1;

            // 限制历史记录数量
            if (_history.Count > 50)
            {
                _history.RemoveAt(0);
                _historyIndex--;
            }
        }

        private void RestoreFromHistory(SelectionHistory history)
        {
            _pointA = history.PointA;
            _pointB = history.PointB;
            _shadowTopPoint = history.ShadowTopPoint;
            _shadowBottomPoint = history.ShadowBottomPoint;
            _measurementMode = history.MeasurementMode;
            _selectionState = history.SelectionState;
            Invalidate();
        }

        #endregion

        #region 事件触发器

        protected virtual void OnPointASelected(PointSelectedEventArgs e)
        {
            PointASelected?.Invoke(this, e);
        }

        protected virtual void OnPointBSelected(PointSelectedEventArgs e)
        {
            PointBSelected?.Invoke(this, e);
        }

        protected virtual void OnSelectionStateChanged(SelectionStateChangedEventArgs e)
        {
            SelectionStateChanged?.Invoke(this, e);
        }

        protected virtual void OnShadowMeasurementCompleted(ShadowMeasurementCompletedEventArgs e)
        {
            ShadowMeasurementCompleted?.Invoke(this, e);
        }

        protected virtual void OnMeasurementModeChanged(MeasurementModeChangedEventArgs e)
        {
            MeasurementModeChanged?.Invoke(this, e);
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 选择历史记录
        /// </summary>
        private class SelectionHistory
        {
            public Point? PointA { get; set; }
            public Point? PointB { get; set; }
            public Point? ShadowTopPoint { get; set; }
            public Point? ShadowBottomPoint { get; set; }
            public MeasurementMode MeasurementMode { get; set; }
            public SelectionState SelectionState { get; set; }
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 点选择事件参数
    /// </summary>
    public class PointSelectedEventArgs : EventArgs
    {
        public Point Location { get; }
        public double Price { get; }

        public PointSelectedEventArgs(Point location, double price)
        {
            Location = location;
            Price = price;
        }
    }

    /// <summary>
    /// 选择状态改变事件参数
    /// </summary>
    public class SelectionStateChangedEventArgs : EventArgs
    {
        public ImprovedKLineSelector.SelectionState OldState { get; }
        public ImprovedKLineSelector.SelectionState NewState { get; }

        public SelectionStateChangedEventArgs(ImprovedKLineSelector.SelectionState oldState, ImprovedKLineSelector.SelectionState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 测量模式改变事件参数
    /// </summary>
    public class MeasurementModeChangedEventArgs : EventArgs
    {
        public ImprovedKLineSelector.MeasurementMode OldMode { get; }
        public ImprovedKLineSelector.MeasurementMode NewMode { get; }

        public MeasurementModeChangedEventArgs(ImprovedKLineSelector.MeasurementMode oldMode, ImprovedKLineSelector.MeasurementMode newMode)
        {
            OldMode = oldMode;
            NewMode = newMode;
        }
    }

    /// <summary>
    /// 影线测量完成事件参数
    /// </summary>
    public class ShadowMeasurementCompletedEventArgs : EventArgs
    {
        public string MeasurementType { get; }
        public Point TopPoint { get; }
        public Point BottomPoint { get; }
        public double TopPrice { get; }
        public double BottomPrice { get; }
        public double ReferencePrice { get; }

        public ShadowMeasurementCompletedEventArgs(string measurementType, Point topPoint, Point bottomPoint,
            double topPrice, double bottomPrice, double referencePrice)
        {
            MeasurementType = measurementType;
            TopPoint = topPoint;
            BottomPoint = bottomPoint;
            TopPrice = topPrice;
            BottomPrice = bottomPrice;
            ReferencePrice = referencePrice;
        }
    }

    #endregion
}