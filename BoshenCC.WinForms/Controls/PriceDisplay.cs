using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using BoshenCC.Models;

namespace BoshenCC.WinForms.Controls
{
    /// <summary>
    /// 价格显示控件
    /// 实时显示A点、B点价格和预测线价格信息
    /// </summary>
    public class PriceDisplay : Control
    {
        #region 私有字段

        private double? _pointAPrice;
        private double? _pointBPrice;
        private List<PredictionLine> _predictionLines;
        private readonly Dictionary<int, Color> _lineColors;
        private readonly Font _titleFont;
        private readonly Font _priceFont;
        private readonly Font _labelFont;
        private readonly SolidBrush _textBrush;
        private readonly SolidBrush _titleBrush;
        private readonly SolidBrush _backgroundBrush;
        private readonly Pen _borderPen;

        #endregion

        #region 构造函数

        public PriceDisplay()
        {
            InitializeComponent();

            _predictionLines = new List<PredictionLine>();

            // 初始化线条颜色
            _lineColors = new Dictionary<int, Color>
            {
                { 1, Color.Blue }, { 2, Color.Blue }, { 3, Color.Red },
                { 4, Color.Blue }, { 5, Color.Blue }, { 6, Color.Red },
                { 7, Color.Blue }, { 8, Color.Red }, { 9, Color.Blue },
                { 10, Color.Blue }, { 11, Color.Blue }
            };

            // 初始化字体和画笔
            _titleFont = new Font("Arial", 10, FontStyle.Bold);
            _priceFont = new Font("Consolas", 9, FontStyle.Bold);
            _labelFont = new Font("Arial", 8);
            _textBrush = new SolidBrush(Color.FromArgb(51, 51, 51));
            _titleBrush = new SolidBrush(Color.FromArgb(0, 122, 204));
            _backgroundBrush = new SolidBrush(Color.FromArgb(248, 249, 250));
            _borderPen = new Pen(Color.FromArgb(204, 204, 204));

            // 启用双缓冲
            DoubleBuffered = true;
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// A点价格
        /// </summary>
        public double? PointAPrice
        {
            get => _pointAPrice;
            set
            {
                if (_pointAPrice != value)
                {
                    _pointAPrice = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// B点价格
        /// </summary>
        public double? PointBPrice
        {
            get => _pointBPrice;
            set
            {
                if (_pointBPrice != value)
                {
                    _pointBPrice = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 预测线集合
        /// </summary>
        public List<PredictionLine> PredictionLines
        {
            get => _predictionLines;
            set
            {
                _predictionLines = value ?? new List<PredictionLine>();
                Invalidate();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 更新A点价格
        /// </summary>
        /// <param name="price">价格值</param>
        public void UpdatePointAPrice(double price)
        {
            PointAPrice = price;
        }

        /// <summary>
        /// 更新B点价格
        /// </summary>
        /// <param name="price">价格值</param>
        public void UpdatePointBPrice(double price)
        {
            PointBPrice = price;
        }

        /// <summary>
        /// 更新预测线
        /// </summary>
        /// <param name="predictionLines">预测线集合</param>
        public void UpdatePredictionLines(List<PredictionLine> predictionLines)
        {
            PredictionLines = predictionLines;
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearAll()
        {
            _pointAPrice = null;
            _pointBPrice = null;
            _predictionLines.Clear();
            Invalidate();
        }

        /// <summary>
        /// 设置重点线颜色
        /// </summary>
        /// <param name="lineNumber">线号</param>
        /// <param name="color">颜色</param>
        public void SetLineColor(int lineNumber, Color color)
        {
            _lineColors[lineNumber] = color;
            Invalidate();
        }

        #endregion

        #region 重写方法

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制背景
            e.Graphics.FillRectangle(_backgroundBrush, ClientRectangle);
            e.Graphics.DrawRectangle(_borderPen, 0, 0, Width - 1, Height - 1);

            // 绘制内容
            var y = 10;
            y = DrawTitle(e.Graphics, "价格信息", y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawSelectionPoints(e.Graphics, y);
            y = DrawSectionSeparator(e.Graphics, y);
            y = DrawPredictionLines(e.Graphics, y);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont?.Dispose();
                _priceFont?.Dispose();
                _labelFont?.Dispose();
                _textBrush?.Dispose();
                _titleBrush?.Dispose();
                _backgroundBrush?.Dispose();
                _borderPen?.Dispose();
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

            BackColor = Color.FromArgb(248, 249, 250);
            ForeColor = Color.FromArgb(51, 51, 51);
            MinimumSize = new Size(200, 300);
            Padding = new Padding(10);
        }

        private int DrawTitle(Graphics g, string title, int y)
        {
            var titleSize = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, _titleBrush, 10, y);
            return y + (int)titleSize.Height + 10;
        }

        private int DrawSectionSeparator(Graphics g, int y)
        {
            using (var separatorPen = new Pen(Color.FromArgb(230, 230, 230), 1))
            {
                separatorPen.DashStyle = DashStyle.Dash;
                g.DrawLine(separatorPen, 10, y + 5, Width - 10, y + 5);
            }
            return y + 15;
        }

        private int DrawSelectionPoints(Graphics g, int y)
        {
            // 绘制A点价格
            if (_pointAPrice.HasValue)
            {
                g.DrawString("A点价格:", _labelFont, _textBrush, 10, y);
                var priceText = FormatPrice(_pointAPrice.Value);
                g.DrawString(priceText, _priceFont, _textBrush, 80, y);
                y += 20;
            }

            // 绘制B点价格
            if (_pointBPrice.HasValue)
            {
                g.DrawString("B点价格:", _labelFont, _textBrush, 10, y);
                var priceText = FormatPrice(_pointBPrice.Value);
                g.DrawString(priceText, _priceFont, _textBrush, 80, y);
                y += 20;
            }

            // 如果没有选择点，显示提示
            if (!_pointAPrice.HasValue && !_pointBPrice.HasValue)
            {
                g.DrawString("请选择K线点位", _labelFont, Brushes.Gray, 10, y);
                y += 20;
            }

            return y + 10;
        }

        private int DrawPredictionLines(Graphics g, int y)
        {
            if (_predictionLines.Count == 0)
            {
                g.DrawString("暂无预测线数据", _labelFont, Brushes.Gray, 10, y);
                return y + 20;
            }

            // 绘制预测线标题
            g.DrawString("预测线价格:", _labelFont, _textBrush, 10, y);
            y += 20;

            // 绘制每条预测线
            foreach (var line in _predictionLines)
            {
                y = DrawPredictionLine(g, line, y);
            }

            return y;
        }

        private int DrawPredictionLine(Graphics g, PredictionLine line, int y)
        {
            var lineNumber = line.LineNumber;
            var price = line.Price;
            var isImportant = lineNumber == 3 || lineNumber == 6 || lineNumber == 8;

            // 获取线条颜色
            var color = _lineColors.ContainsKey(lineNumber) ? _lineColors[lineNumber] : Color.Blue;

            // 绘制线条指示器
            var indicatorRect = new Rectangle(10, y + 2, 12, 12);
            using (var indicatorBrush = new SolidBrush(color))
            {
                if (isImportant)
                {
                    // 重点线使用实心矩形
                    g.FillRectangle(indicatorBrush, indicatorRect);
                    g.DrawRectangle(Pens.Black, indicatorRect);
                }
                else
                {
                    // 普通线使用空心矩形
                    g.DrawRectangle(new Pen(color), indicatorRect);
                }
            }

            // 绘制线号
            var labelText = $"第{lineNumber}线:";
            g.DrawString(labelText, _labelFont, _textBrush, 30, y);

            // 绘制价格
            var priceText = FormatPrice(price);
            var priceSize = g.MeasureString(priceText, _priceFont);
            g.DrawString(priceText, _priceFont,
                isImportant ? new SolidBrush(color) : _textBrush,
                80, y);

            // 如果是重点线，添加星号标记
            if (isImportant)
            {
                g.DrawString("*", _labelFont, new SolidBrush(color),
                    80 + (int)priceSize.Width + 5, y);
            }

            return y + 18;
        }

        private string FormatPrice(double price)
        {
            return price.ToString("F2");
        }

        #endregion
    }
}