using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// K线选择控件
    /// 支持精确点击选择A点和B点
    /// </summary>
    public class KLineSelector : Control
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

        #endregion

        #region 私有字段

        private CoordinateHelper _coordinateHelper;
        private Point? _pointA;
        private Point? _pointB;
        private SelectionState _selectionState;
        private bool _isHovering;
        private Point _mousePosition;
        private Image _backgroundImage;
        private readonly Pen _pointAPen;
        private readonly Pen _pointBPen;
        private readonly SolidBrush _pointABrush;
        private readonly SolidBrush _pointBBrush;
        private readonly Font _tooltipFont;
        private readonly SolidBrush _tooltipBrush;

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

        #endregion

        #region 构造函数

        public KLineSelector()
        {
            InitializeComponent();

            _coordinateHelper = new CoordinateHelper();
            _selectionState = SelectionState.None;

            // 初始化绘图资源
            _pointAPen = new Pen(Color.Lime, 2);
            _pointBPen = new Pen(Color.Red, 2);
            _pointABrush = new SolidBrush(Color.Lime);
            _pointBBrush = new SolidBrush(Color.Red);
            _tooltipFont = new Font("Arial", 9);
            _tooltipBrush = new SolidBrush(Color.Black);

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
        /// 检查选择是否完成
        /// </summary>
        /// <returns>是否已完成A点和B点选择</returns>
        public bool IsSelectionComplete()
        {
            return _pointA.HasValue && _pointB.HasValue;
        }

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

            // 绘制选择点
            DrawSelectionPoints(e.Graphics);

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pointAPen?.Dispose();
                _pointBPen?.Dispose();
                _pointABrush?.Dispose();
                _pointBBrush?.Dispose();
                _tooltipFont?.Dispose();
                _tooltipBrush?.Dispose();
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
                    // 重新开始选择
                    ClearSelection();
                    _pointA = location;
                    OnPointASelected(new PointSelectedEventArgs(location, _coordinateHelper.YToPrice(location.Y)));
                    break;
            }

            UpdateSelectionState();
            Invalidate();
        }

        private void UpdateSelectionState()
        {
            if (_pointA.HasValue && _pointB.HasValue)
            {
                CurrentState = SelectionState.Complete;
            }
            else if (_pointA.HasValue)
            {
                CurrentState = SelectionState.PointASelected;
            }
            else
            {
                CurrentState = SelectionState.None;
            }
        }

        private void DrawSelectionPoints(Graphics g)
        {
            const int pointSize = 8;

            if (_pointA.HasValue)
            {
                var rect = new Rectangle(
                    _pointA.Value.X - pointSize / 2,
                    _pointA.Value.Y - pointSize / 2,
                    pointSize,
                    pointSize);

                g.FillEllipse(_pointABrush, rect);
                g.DrawEllipse(_pointAPen, rect);

                // 绘制A点标签
                var label = "A";
                var labelSize = g.MeasureString(label, _tooltipFont);
                g.DrawString(label, _tooltipFont, _pointABrush,
                    _pointA.Value.X + pointSize, _pointA.Value.Y - labelSize.Height);
            }

            if (_pointB.HasValue)
            {
                var rect = new Rectangle(
                    _pointB.Value.X - pointSize / 2,
                    _pointB.Value.Y - pointSize / 2,
                    pointSize,
                    pointSize);

                g.FillEllipse(_pointBBrush, rect);
                g.DrawEllipse(_pointBPen, rect);

                // 绘制B点标签
                var label = "B";
                var labelSize = g.MeasureString(label, _tooltipFont);
                g.DrawString(label, _tooltipFont, _pointBBrush,
                    _pointB.Value.X + pointSize, _pointB.Value.Y - labelSize.Height);
            }
        }

        private void DrawTooltip(Graphics g)
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
        public KLineSelector.SelectionState OldState { get; }
        public KLineSelector.SelectionState NewState { get; }

        public SelectionStateChangedEventArgs(KLineSelector.SelectionState oldState, KLineSelector.SelectionState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    #endregion
}